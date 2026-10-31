#if UNITY_EDITOR
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;

namespace Pretzel.Multiplayer.PlayModeTests
{
    /// <summary>Prefabs that a NetworkPrefabsList already holds must not be registered a second time by the bootstrap (NGO logs an error for duplicates).</summary>
    public sealed class PrefabListRegistrationTests
    {
        private const string TempFolder = "Assets/PretzelMultiplayerListTestTemp";

        private GameObject _root;
        private PretzelNetworkBootstrap _bootstrap;
        private NetworkManager _manager;
        private NetworkPrefabsList _list;
        private GameObject _playerPrefab;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameObject player = new GameObject("ListPlayer", typeof(NetworkObject), typeof(NetworkPlayerIdentity));
            _playerPrefab = TestPrefabs.Save(player, TempFolder + "/ListPlayer.prefab");
            GameObject item = new GameObject("ListItem", typeof(NetworkObject), typeof(PoolProbe));
            GameObject itemPrefab = TestPrefabs.Save(item, TempFolder + "/ListItem.prefab");

            _list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            _list.Add(new NetworkPrefab { Prefab = _playerPrefab });
            _list.Add(new NetworkPrefab { Prefab = itemPrefab });

            _root = new GameObject("NetworkManager");
            _root.SetActive(false);
            _manager = _root.AddComponent<NetworkManager>();
            UnityTransport transport = _root.AddComponent<UnityTransport>();
            _manager.NetworkConfig = new NetworkConfig { NetworkTransport = transport };
            _manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(_list);
            _bootstrap = _root.AddComponent<PretzelNetworkBootstrap>();
            TestPrefabs.Configure(_bootstrap, _playerPrefab, itemPrefab, 1, 4);
            _root.SetActive(true);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_bootstrap != null && _bootstrap.Service != null && _bootstrap.Service.State != ConnectionState.Disconnected)
            {
                Task leave = _bootstrap.Service.LeaveAsync();
                while (!leave.IsCompleted)
                {
                    yield return null;
                }
            }

            if (_root != null)
            {
                Object.Destroy(_root);
            }

            if (_list != null)
            {
                Object.Destroy(_list);
            }

            yield return null;
            TestPrefabs.Delete(TempFolder);
        }

        [UnityTest]
        public IEnumerator PrefabsAlreadyInAList_AreNotRegisteredTwice()
        {
            Task<bool> start = _bootstrap.Service.StartLocalAsync(new HostRequest { LocalPlayerCount = 1 });
            float deadline = Time.realtimeSinceStartup + 15f;
            while (!start.IsCompleted && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(start.IsCompleted && start.Result, "The session starts without a duplicate registration error");
            int registrations = 0;
            foreach (NetworkPrefab prefab in _manager.NetworkConfig.Prefabs.Prefabs)
            {
                if (prefab.Prefab == _playerPrefab)
                {
                    registrations++;
                }
            }

            Assert.AreEqual(1, registrations);
        }
    }
}
#endif
