using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Pretzel.Multiplayer
{
    /// <summary>
    /// The composition root. Put it next to the NetworkManager (<c>GameObject > Pretzel > Multiplayer > Network Manager (Pretzel defaults)</c> does that).
    /// It applies <see cref="NetworkSettings"/> to the NetworkManager and UnityTransport, registers pooled prefab handlers, creates the
    /// <see cref="IMultiplayerSessionService"/> and runs server-side player spawning. Everything it subscribes to is released in OnDestroy.
    /// </summary>
    [AddComponentMenu("Pretzel/Multiplayer/Network Bootstrap")]
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class PretzelNetworkBootstrap : MonoBehaviour
    {
        [SerializeField] private NetworkManager _networkManager;
        [SerializeField] private NetworkSettings _settings = new NetworkSettings();

        [Header("Players")]
        [Tooltip("Spawned once per local player of every connected client. Needs a NetworkObject and ideally a NetworkPlayerIdentity.")]
        [SerializeField] private GameObject _playerPrefab;

        [Tooltip("Optional. Slot N spawns at spawn point N (wrapping). Without points the players start in a row along X.")]
        [SerializeField] private Transform[] _spawnPoints = new Transform[0];

        [Header("Pooling")]
        [Tooltip("Network prefabs that are spawned from a pool.")]
        [SerializeField] private PooledPrefabEntry[] _pooledPrefabs = new PooledPrefabEntry[0];

        [Header("Rejoin")]
        [Tooltip("On start, offer to rejoin a session that was remembered when the app closed or the connection dropped.")]
        [SerializeField] private bool _offerRejoinOnStart = true;

        private readonly List<PooledNetworkPrefabHandler> _handlers = new List<PooledNetworkPrefabHandler>();
        private readonly List<PooledPrefabEntry> _registeredEntries = new List<PooledPrefabEntry>();
        private MultiplayerSessionService _service;
        private ServerPlayerSpawner _spawner;
        private bool _initialized;

        public IMultiplayerSessionService Service => _service;

        public NetworkSettings Settings => _settings;

        public NetworkManager NetworkManager => _networkManager;

        public ServerPlayerSpawner Spawner => _spawner;

        public IReadOnlyList<PooledNetworkPrefabHandler> PooledHandlers => _handlers;

        private void Awake()
        {
            if (_networkManager == null)
            {
                _networkManager = GetComponent<NetworkManager>();
            }

            if (_networkManager == null)
            {
                _networkManager = FindAnyObjectByType<NetworkManager>();
            }

            if (_networkManager == null || _networkManager.NetworkConfig == null)
            {
                Debug.LogError("[Pretzel.Multiplayer] No NetworkManager with a NetworkConfig found. Add one, or use GameObject > Pretzel > Multiplayer > Network Manager (Pretzel defaults).", this);
                enabled = false;
                return;
            }

            IReadOnlyList<string> problems = _settings.Validate();
            if (problems.Count > 0)
            {
                Debug.LogError("[Pretzel.Multiplayer] Invalid NetworkSettings: " + string.Join(" ", problems), this);
                enabled = false;
                return;
            }

            ApplySettings();
            _spawner = new ServerPlayerSpawner(_networkManager, _settings, _playerPrefab);
            _spawner.SpawnPosition = GetSpawnPosition;
            _spawner.Enable();
            EnsureRegistered(_playerPrefab);
            RegisterPooledPrefabs();
            _service = new MultiplayerSessionService(_settings, CreateBackend, new PlayerPrefsReconnectStore());
            _service.StateChanged += OnStateChanged;
            _initialized = true;
        }

        private void Start()
        {
            if (!_initialized)
            {
                return;
            }

            for (int i = 0; i < _handlers.Count; i++)
            {
                _handlers[i].Prewarm(_registeredEntries[i].Prewarm);
            }

            if (_offerRejoinOnStart)
            {
                _service.CheckForPendingRejoin();
            }
        }

        private void Update()
        {
            _spawner?.Tick();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                _service?.NotifyApplicationResumed();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused)
            {
                _service?.NotifyApplicationResumed();
            }
        }

        private void OnDestroy()
        {
            if (_service != null)
            {
                _service.StateChanged -= OnStateChanged;
                _service.Dispose();
                _service = null;
            }

            _spawner?.Dispose();
            _spawner = null;
            for (int i = 0; i < _handlers.Count; i++)
            {
                _handlers[i].Dispose();
            }

            _handlers.Clear();
            _registeredEntries.Clear();
        }

        private void ApplySettings()
        {
            NetworkConfig config = _networkManager.NetworkConfig;
            config.TickRate = (uint)_settings.TickRate;
            config.ProtocolVersion = _settings.ProtocolVersion;
            config.ClientConnectionBufferTimeout = _settings.ApprovalTimeoutSeconds;
            config.ConnectionApproval = true;
            UnityTransport transport = config.NetworkTransport as UnityTransport;
            if (transport == null)
            {
                return;
            }

            if (_settings.ConnectTimeoutMs > 0)
            {
                transport.ConnectTimeoutMS = _settings.ConnectTimeoutMs;
            }

            if (_settings.MaxConnectAttempts > 0)
            {
                transport.MaxConnectAttempts = _settings.MaxConnectAttempts;
            }

            if (_settings.DisconnectTimeoutMs > 0)
            {
                transport.DisconnectTimeoutMS = _settings.DisconnectTimeoutMs;
            }
        }

        private void EnsureRegistered(GameObject prefab)
        {
            // Prefab lists are only merged into NetworkConfig.Prefabs when the NetworkManager initializes, so Contains alone misses them and registering twice logs an error.
            if (prefab != null && !_networkManager.NetworkConfig.Prefabs.Contains(prefab) && !IsInPrefabList(prefab))
            {
                _networkManager.AddNetworkPrefab(prefab);
            }
        }

        private bool IsInPrefabList(GameObject prefab)
        {
            foreach (NetworkPrefabsList list in _networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists)
            {
                if (list != null && list.Contains(prefab))
                {
                    return true;
                }
            }

            return false;
        }

        private void RegisterPooledPrefabs()
        {
            foreach (PooledPrefabEntry entry in _pooledPrefabs)
            {
                NetworkObject networkObject = entry?.Prefab != null ? entry.Prefab.GetComponent<NetworkObject>() : null;
                if (networkObject == null)
                {
                    Debug.LogWarning("[Pretzel.Multiplayer] A pooled prefab entry is empty or has no NetworkObject and was skipped.", this);
                    continue;
                }

                EnsureRegistered(entry.Prefab);
                PooledNetworkPrefabHandler handler = new PooledNetworkPrefabHandler(networkObject, Mathf.Max(1, entry.Prewarm), entry.MaxIdle);
                if (!handler.Register(_networkManager))
                {
                    Debug.LogWarning($"[Pretzel.Multiplayer] '{entry.Prefab.name}' already has a prefab handler and was skipped.", this);
                    handler.Dispose();
                    continue;
                }

                _handlers.Add(handler);
                _registeredEntries.Add(entry);
            }
        }

        private ISessionBackend CreateBackend(SessionMode mode)
        {
            return mode == SessionMode.Local ? new LocalSessionBackend(_networkManager, _settings) : new OnlineSessionBackend(_networkManager, _settings);
        }

        private void OnStateChanged(ConnectionState previous, ConnectionState current)
        {
            if (current == ConnectionState.Playing)
            {
                _spawner.SetMatchStarted(true);
            }
            else if (current == ConnectionState.InLobby)
            {
                _spawner.SetMatchStarted(false);
            }
        }

        private Vector3 GetSpawnPosition(int slot)
        {
            if (_spawnPoints != null && _spawnPoints.Length > 0)
            {
                Transform point = _spawnPoints[slot % _spawnPoints.Length];
                if (point != null)
                {
                    return point.position;
                }
            }

            return new Vector3(slot * 2.5f - 1.25f, 0f, 0f);
        }
    }
}
