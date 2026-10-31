using System;
using System.Collections.Generic;
using Pretzel.Core;
using Unity.Netcode;
using UnityEngine;

namespace Pretzel.Multiplayer
{
    /// <summary>
    /// Server-side connection approval and player spawning. Approves or rejects each client (payload size and format, max players, duplicate key, free slots),
    /// spawns one owned player object per local player, keeps the objects of a dropped client for the grace window and hands them back when the same player key reconnects.
    /// Does nothing on clients. Subscriptions are removed in <see cref="Disable"/>.
    /// </summary>
    public sealed class ServerPlayerSpawner : IDisposable
    {
        private readonly NetworkManager _networkManager;
        private readonly NetworkSettings _settings;
        private readonly PlayerSlotRegistry _slots;
        private readonly Dictionary<ulong, ConnectionPayload> _approved = new Dictionary<ulong, ConnectionPayload>();
        private readonly Dictionary<ulong, ConnectionPayload> _connected = new Dictionary<ulong, ConnectionPayload>();
        private readonly HashSet<ulong> _spawned = new HashSet<ulong>();
        private readonly List<PlayerSlotReservation> _expired = new List<PlayerSlotReservation>();
        private readonly List<ulong> _scratch = new List<ulong>();
        private GameObject _playerPrefab;
        private bool _matchStarted;
        private bool _enabled;

        public PlayerSlotRegistry Slots => _slots;

        /// <summary>Position for a player object by slot. Defaults to a row along X.</summary>
        public Func<int, Vector3> SpawnPosition { get; set; } = slot => new Vector3(slot * 2.5f - 1.25f, 0f, 0f);

        public GameObject PlayerPrefab
        {
            get => _playerPrefab;
            set => _playerPrefab = value;
        }

        public ServerPlayerSpawner(NetworkManager networkManager, NetworkSettings settings, GameObject playerPrefab, Func<DateTime> utcNow = null)
        {
            _networkManager = Guard.NotDestroyed(networkManager, nameof(networkManager));
            _settings = Guard.NotNull(settings, nameof(settings));
            _playerPrefab = playerPrefab;
            _slots = new PlayerSlotRegistry(settings.MaxPlayerObjects, settings.ReconnectGrace, utcNow);
        }

        public void Enable()
        {
            if (_enabled)
            {
                return;
            }

            _enabled = true;
            _networkManager.NetworkConfig.ConnectionApproval = true;
            if (_networkManager.ConnectionApprovalCallback != null)
            {
                Debug.LogWarning("[Pretzel.Multiplayer] Replacing the existing NetworkManager.ConnectionApprovalCallback.");
            }

            _networkManager.ConnectionApprovalCallback = OnApproval;
            _networkManager.OnClientConnectedCallback += OnClientConnected;
            _networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            _networkManager.OnServerStopped += OnServerStopped;
        }

        public void Disable()
        {
            if (!_enabled)
            {
                return;
            }

            _enabled = false;
            if (_networkManager != null)
            {
                if (_networkManager.ConnectionApprovalCallback == (Action<NetworkManager.ConnectionApprovalRequest, NetworkManager.ConnectionApprovalResponse>)OnApproval)
                {
                    _networkManager.ConnectionApprovalCallback = null;
                }

                _networkManager.OnClientConnectedCallback -= OnClientConnected;
                _networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
                _networkManager.OnServerStopped -= OnServerStopped;
            }

            Reset();
        }

        public void Dispose()
        {
            Disable();
        }

        /// <summary>Tells the spawner the match runs. With <see cref="PlayerSpawnTiming.OnMatchStart"/> it spawns for everyone connected and for later joiners.</summary>
        public void SetMatchStarted(bool started)
        {
            _matchStarted = started;
            if (!started || !_networkManager.IsServer || _settings.SpawnTiming != PlayerSpawnTiming.OnMatchStart)
            {
                return;
            }

            _scratch.Clear();
            foreach (KeyValuePair<ulong, ConnectionPayload> pair in _connected)
            {
                _scratch.Add(pair.Key);
            }

            for (int i = 0; i < _scratch.Count; i++)
            {
                TrySpawn(_scratch[i]);
            }
        }

        /// <summary>Call every frame while hosting. Costs nothing unless a dropped player is waiting.</summary>
        public void Tick()
        {
            if (!_slots.HasReservations || !_networkManager.IsServer)
            {
                return;
            }

            _expired.Clear();
            _slots.CollectExpired(_expired);
            for (int i = 0; i < _expired.Count; i++)
            {
                DespawnObjects(_expired[i]);
            }
        }

        private void OnApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.CreatePlayerObject = false;
            bool isHost = request.ClientNetworkId == NetworkManager.ServerClientId;
            if (!ConnectionPayloadCodec.TryDecode(request.Payload, _settings.MaxConnectionDataBytes, _settings.MaxLocalPlayersPerClient, out ConnectionPayload payload, out string error))
            {
                Reject(response, error);
                return;
            }

            if (!isHost)
            {
                if (_networkManager.ConnectedClientsIds.Count >= _settings.MaxPlayers)
                {
                    Reject(response, "The session is full.");
                    return;
                }

                if (_slots.TryGetByKey(payload.PlayerKey, out PlayerSlotReservation known))
                {
                    if (known.IsConnected)
                    {
                        Reject(response, "This player is already connected.");
                        return;
                    }
                }
                else if (_slots.Capacity - _slots.UsedSlots < payload.LocalPlayerCount)
                {
                    Reject(response, "There are no free player slots.");
                    return;
                }
            }

            _approved[request.ClientNetworkId] = payload;
            response.Approved = true;
            response.Pending = false;
        }

        private void OnClientConnected(ulong clientId)
        {
            if (!_networkManager.IsServer || !_approved.TryGetValue(clientId, out ConnectionPayload payload))
            {
                return;
            }

            _approved.Remove(clientId);
            PlayerSlotClaimResult claim = _slots.Claim(payload.PlayerKey, clientId, payload.LocalPlayerCount);
            if (!claim.Succeeded)
            {
                if (clientId != _networkManager.LocalClientId)
                {
                    _networkManager.DisconnectClient(clientId, claim.Status == PlayerSlotClaimStatus.Full ? "There are no free player slots." : "This player is already connected.");
                }

                return;
            }

            _connected[clientId] = payload;
            TrySpawn(clientId);
        }

        private void OnClientDisconnected(ulong clientId)
        {
            _approved.Remove(clientId);
            _spawned.Remove(clientId);
            if (!_connected.Remove(clientId) || !_networkManager.IsServer || _networkManager.ShutdownInProgress || _networkManager.SpawnManager == null
                || clientId == _networkManager.LocalClientId)
            {
                return;
            }

            if (!_slots.TryGetByClient(clientId, out PlayerSlotReservation reservation))
            {
                return;
            }

            PlayerSlotReservation released = _slots.Release(clientId);
            if (released != null)
            {
                DespawnObjects(released);
                return;
            }

            SetController(reservation, NetworkPlayerIdentity.NoController);
        }

        private void OnServerStopped(bool wasHost)
        {
            Reset();
        }

        private void TrySpawn(ulong clientId)
        {
            if (_playerPrefab == null || _spawned.Contains(clientId) || !_connected.TryGetValue(clientId, out ConnectionPayload payload))
            {
                return;
            }

            if (_settings.SpawnTiming == PlayerSpawnTiming.OnMatchStart && !_matchStarted)
            {
                return;
            }

            if (!_slots.TryGetByClient(clientId, out PlayerSlotReservation reservation))
            {
                return;
            }

            _spawned.Add(clientId);
            if (reservation.ObjectIds.Count > 0)
            {
                ReassignObjects(reservation, clientId);
                return;
            }

            for (int i = 0; i < reservation.Slots.Count; i++)
            {
                int slot = reservation.Slots[i];
                NetworkObject instance = NetworkObject.InstantiateAndSpawn(_playerPrefab, _networkManager, clientId, false, false, false, SpawnPosition(slot), Quaternion.identity);
                if (instance == null)
                {
                    Debug.LogWarning($"[Pretzel.Multiplayer] Could not spawn the player prefab for client {clientId}. Is it in the NetworkManager prefab list?");
                    continue;
                }

                // Server-side only: NGO reads this at disconnect, so the object stays and the owner reverts to the server.
                instance.DontDestroyWithOwner = true;
                NetworkPlayerIdentity identity = instance.GetComponent<NetworkPlayerIdentity>();
                if (identity != null)
                {
                    identity.ServerSetIdentity(slot, i, clientId);
                }

                _slots.AddObject(payload.PlayerKey, instance.NetworkObjectId);
            }
        }

        private void ReassignObjects(PlayerSlotReservation reservation, ulong clientId)
        {
            for (int i = 0; i < reservation.ObjectIds.Count; i++)
            {
                if (!_networkManager.SpawnManager.SpawnedObjects.TryGetValue(reservation.ObjectIds[i], out NetworkObject instance) || !instance || !instance.IsSpawned)
                {
                    continue;
                }

                instance.ChangeOwnership(clientId);
                NetworkPlayerIdentity identity = instance.GetComponent<NetworkPlayerIdentity>();
                if (identity != null)
                {
                    identity.ServerSetController(clientId);
                }
            }
        }

        private void SetController(PlayerSlotReservation reservation, ulong controller)
        {
            for (int i = 0; i < reservation.ObjectIds.Count; i++)
            {
                if (_networkManager.SpawnManager.SpawnedObjects.TryGetValue(reservation.ObjectIds[i], out NetworkObject instance) && instance && instance.IsSpawned)
                {
                    NetworkPlayerIdentity identity = instance.GetComponent<NetworkPlayerIdentity>();
                    if (identity != null)
                    {
                        identity.ServerSetController(controller);
                    }
                }
            }
        }

        private void DespawnObjects(PlayerSlotReservation reservation)
        {
            for (int i = 0; i < reservation.ObjectIds.Count; i++)
            {
                if (_networkManager.SpawnManager != null
                    && _networkManager.SpawnManager.SpawnedObjects.TryGetValue(reservation.ObjectIds[i], out NetworkObject instance) && instance && instance.IsSpawned)
                {
                    instance.Despawn(true);
                }
            }
        }

        private void Reset()
        {
            _slots.Clear();
            _approved.Clear();
            _connected.Clear();
            _spawned.Clear();
            _matchStarted = false;
        }

        private static void Reject(NetworkManager.ConnectionApprovalResponse response, string reason)
        {
            response.Approved = false;
            response.Reason = reason;
            response.Pending = false;
        }
    }
}
