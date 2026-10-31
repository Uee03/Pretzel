#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;

namespace Pretzel.Multiplayer.PlayModeTests
{
    /// <summary>
    /// Runs a real NetworkManager host on loopback inside the test process: local-mode session, player spawning, the pooled spawn handler.
    /// Only the host side is covered; a second client (and therefore rejoin) is not exercised here.
    /// </summary>
    public sealed class LocalLoopbackTests
    {
        private const string TempFolder = "Assets/PretzelMultiplayerTestTemp";

        private GameObject _root;
        private NetworkManager _manager;
        private PretzelNetworkBootstrap _bootstrap;
        private GameObject _playerPrefab;
        private GameObject _itemPrefab;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameObject player = new GameObject("TestPlayer", typeof(NetworkObject), typeof(NetworkPlayerIdentity));
            _playerPrefab = TestPrefabs.Save(player, TempFolder + "/TestPlayer.prefab");
            GameObject item = new GameObject("TestItem", typeof(NetworkObject), typeof(PoolProbe));
            _itemPrefab = TestPrefabs.Save(item, TempFolder + "/TestItem.prefab");

            // Inactive while configured so the bootstrap's Awake sees the serialized fields.
            _root = new GameObject("NetworkManager");
            _root.SetActive(false);
            _manager = _root.AddComponent<NetworkManager>();
            UnityTransport transport = _root.AddComponent<UnityTransport>();
            _manager.NetworkConfig = new NetworkConfig { NetworkTransport = transport };
            _bootstrap = _root.AddComponent<PretzelNetworkBootstrap>();
            TestPrefabs.Configure(_bootstrap, _playerPrefab, _itemPrefab, 2, 8);
            _root.SetActive(true);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_bootstrap != null && _bootstrap.Service != null && _bootstrap.Service.State != ConnectionState.Disconnected)
            {
                Task leave = _bootstrap.Service.LeaveAsync();
                yield return WaitFor(leave);
            }

            if (_root != null)
            {
                Object.Destroy(_root);
            }

            yield return null;
            TestPrefabs.Delete(TempFolder);
        }

        [UnityTest]
        public IEnumerator LocalSession_SpawnsOnePlayerObjectPerLocalPlayer_OwnedByTheHostClient()
        {
            Task<bool> start = _bootstrap.Service.StartLocalAsync(new HostRequest { DisplayName = "Ada", LocalPlayerCount = 2 });
            yield return WaitFor(start);
            Assert.IsTrue(start.Result, "Local host should start");
            Assert.AreEqual(ConnectionState.InLobby, _bootstrap.Service.State);
            Assert.IsTrue(_manager.IsHost);
            Assert.AreEqual(0, CountPlayers(), "Players spawn when the match starts");

            Task<bool> match = _bootstrap.Service.StartMatchAsync();
            yield return WaitFor(match);
            Assert.IsTrue(match.Result);
            yield return null;

            NetworkPlayerIdentity[] players = GetPlayers();
            Assert.AreEqual(2, players.Length);
            bool[] seenIndex = new bool[2];
            foreach (NetworkPlayerIdentity player in players)
            {
                NetworkObject networkObject = player.GetComponent<NetworkObject>();
                Assert.IsTrue(networkObject.IsSpawned);
                Assert.AreEqual(_manager.LocalClientId, networkObject.OwnerClientId, "Local players are owned by the host client");
                Assert.IsTrue(player.IsLocallyControlled);
                Assert.IsTrue(networkObject.IsOwner);
                seenIndex[player.LocalPlayerIndex] = true;
            }

            Assert.IsTrue(seenIndex[0] && seenIndex[1], "Both LocalPlayerIndex values are present");
            Assert.AreNotEqual(players[0].SlotIndex, players[1].SlotIndex);
        }

        [UnityTest]
        public IEnumerator PooledPrefab_SpawnDespawnRecyclesTheSameInstance()
        {
            Task<bool> start = _bootstrap.Service.StartLocalAsync(new HostRequest { LocalPlayerCount = 1 });
            yield return WaitFor(start);
            Assert.IsTrue(start.Result);
            PooledNetworkPrefabHandler handler = _bootstrap.PooledHandlers[0];
            Assert.AreEqual(2, handler.CountInactive, "Prewarmed instances are idle");

            NetworkObject first = NetworkObject.InstantiateAndSpawn(_itemPrefab, _manager, NetworkManager.ServerClientId, false, false, false, new Vector3(1f, 2f, 3f));
            Assert.IsNotNull(first);
            PoolProbe probe = first.GetComponent<PoolProbe>();
            Assert.IsTrue(first.IsSpawned);
            Assert.AreEqual(1, handler.CountActive);
            Assert.AreEqual(1, handler.CountInactive, "One of the prewarmed instances was taken");
            Assert.AreEqual(1, probe.Taken);
            Assert.AreEqual(1, probe.Spawned);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), first.transform.position);
            first.CheckObjectVisibility = id => true;
            first.transform.localScale = Vector3.one * 3f;

            first.Despawn(true);
            yield return null;

            Assert.IsFalse(first.IsSpawned);
            Assert.IsFalse(first.gameObject.activeSelf, "A despawned pooled instance is idle");
            Assert.IsTrue(first, "It was not destroyed");
            Assert.AreEqual(0, handler.CountActive);
            Assert.AreEqual(2, handler.CountInactive);
            Assert.AreEqual(1, probe.Returned);
            Assert.AreEqual(1, probe.Despawned);
            Assert.IsNull(first.CheckObjectVisibility, "NetworkObject state is reset for reuse");
            Assert.AreEqual(_itemPrefab.transform.localScale, first.transform.localScale);

            NetworkObject second = NetworkObject.InstantiateAndSpawn(_itemPrefab, _manager, NetworkManager.ServerClientId, false, false, false, new Vector3(4f, 0f, 0f));
            Assert.AreSame(first, second, "The idle instance is reused");
            Assert.IsTrue(second.IsSpawned);
            Assert.AreEqual(2, probe.Taken);
            Assert.AreEqual(2, probe.Spawned);
            Assert.AreEqual(1, handler.CountActive);
            Assert.AreEqual(new Vector3(4f, 0f, 0f), second.transform.position);
        }

        [UnityTest]
        public IEnumerator PooledPrefab_DespawnAllOnShutdown_ReturnsEverythingToThePool()
        {
            Task<bool> start = _bootstrap.Service.StartLocalAsync(new HostRequest { LocalPlayerCount = 1 });
            yield return WaitFor(start);
            PooledNetworkPrefabHandler handler = _bootstrap.PooledHandlers[0];
            for (int i = 0; i < 3; i++)
            {
                Assert.IsNotNull(NetworkObject.InstantiateAndSpawn(_itemPrefab, _manager, NetworkManager.ServerClientId, false, false, false, new Vector3(i, 0f, 0f)));
            }

            Assert.AreEqual(3, handler.CountActive);

            Task leave = _bootstrap.Service.LeaveAsync();
            yield return WaitFor(leave);
            yield return null;

            Assert.AreEqual(ConnectionState.Disconnected, _bootstrap.Service.State);
            Assert.IsFalse(_manager.IsListening);
            Assert.AreEqual(0, handler.CountActive, "Shutdown despawns through the handler");
            Assert.GreaterOrEqual(handler.CountInactive, 3);
        }

        [UnityTest]
        public IEnumerator LocalSession_CanBeStartedAgainAfterLeaving()
        {
            Task<bool> first = _bootstrap.Service.StartLocalAsync(new HostRequest { LocalPlayerCount = 2 });
            yield return WaitFor(first);
            Assert.IsTrue(first.Result);
            yield return WaitFor(_bootstrap.Service.LeaveAsync());
            yield return null;

            Task<bool> second = _bootstrap.Service.StartLocalAsync(new HostRequest { LocalPlayerCount = 1 });
            yield return WaitFor(second);

            Assert.IsTrue(second.Result, "The loopback port is free again after Leave");
            Assert.AreEqual(ConnectionState.InLobby, _bootstrap.Service.State);
            Assert.AreEqual(1, _bootstrap.Service.Lobby.Players.Count);
        }

        [UnityTest]
        public IEnumerator Service_RejectsMoreLocalPlayersThanConfigured()
        {
            Task<bool> start = _bootstrap.Service.StartLocalAsync(new HostRequest { LocalPlayerCount = _bootstrap.Settings.MaxLocalPlayersPerClient + 1 });
            yield return WaitFor(start);

            Assert.IsFalse(start.Result);
            Assert.AreEqual(ConnectionState.Disconnected, _bootstrap.Service.State);
            Assert.IsFalse(_manager.IsListening);
        }

        private int CountPlayers()
        {
            return GetPlayers().Length;
        }

        private NetworkPlayerIdentity[] GetPlayers()
        {
            List<NetworkPlayerIdentity> found = new List<NetworkPlayerIdentity>();
            foreach (NetworkObject spawned in _manager.SpawnManager.SpawnedObjectsList)
            {
                NetworkPlayerIdentity identity = spawned.GetComponent<NetworkPlayerIdentity>();
                if (identity != null)
                {
                    found.Add(identity);
                }
            }

            return found.ToArray();
        }

        private static IEnumerator WaitFor(Task task, float timeoutSeconds = 15f)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!task.IsCompleted)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail("Timed out waiting for the task.");
                }

                yield return null;
            }

            if (task.IsFaulted)
            {
                throw task.Exception;
            }
        }
    }
}
#endif
