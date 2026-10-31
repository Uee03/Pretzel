using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pretzel.Core;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Pretzel.Multiplayer
{
    /// <summary>
    /// Same-machine couch co-op: a NetworkManager host on loopback with UnityTransport. No cloud, no authentication, no remote clients.
    /// The additional local players are player objects owned by the host client, spawned by <see cref="ServerPlayerSpawner"/> exactly like online players.
    /// </summary>
    public sealed class LocalSessionBackend : ISessionBackend
    {
        public const string LocalPlayerKey = "local";

        private readonly NetworkManager _networkManager;
        private readonly NetworkSettings _settings;
        private LobbySnapshot _snapshot;
        private bool _leaving;
        private bool _running;
        private bool _subscribed;

        public SessionMode Mode => SessionMode.Local;

        public bool IsConnected => _networkManager != null && _networkManager.IsListening && _networkManager.IsServer;

        public event Action<LobbySnapshot> LobbyChanged;

        public event Action<bool> MatchStateChanged;

        public event Action<SessionEndReason, MultiplayerError> SessionEnded;

        /// <summary>Never raised: a local host cannot lose its connection, it can only stop (<see cref="SessionEnded"/>).</summary>
        public event Action<MultiplayerError> ConnectionLost
        {
            add { }
            remove { }
        }

        public LocalSessionBackend(NetworkManager networkManager, NetworkSettings settings)
        {
            _networkManager = Guard.NotDestroyed(networkManager, nameof(networkManager));
            _settings = Guard.NotNull(settings, nameof(settings));
        }

        public async Task<LobbySnapshot> HostAsync(HostRequest request, CancellationToken cancellationToken)
        {
            UnityTransport transport = _networkManager.NetworkConfig.NetworkTransport as UnityTransport;
            if (transport == null)
            {
                throw new MultiplayerException(MultiplayerErrorCode.NetworkManagerMissing, "The NetworkManager needs a UnityTransport component.");
            }

            if (!await NetworkManagerLifecycle.ShutdownAsync(_networkManager, cancellationToken))
            {
                throw new MultiplayerException(MultiplayerErrorCode.InvalidOperation, "The NetworkManager is still shutting down.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            transport.SetConnectionData("127.0.0.1", (ushort)_settings.LoopbackPort, "127.0.0.1");
            ConnectionPayload payload = new ConnectionPayload(LocalPlayerKey, request.DisplayName, request.PlatformTag, request.LocalPlayerCount);
            NetworkManagerLifecycle.SetConnectionData(_networkManager, payload, _settings);
            Subscribe();
            _leaving = false;
            if (!_networkManager.StartHost())
            {
                Unsubscribe();
                throw new MultiplayerException(MultiplayerErrorCode.ConnectionFailed, $"Could not start the local host on port {_settings.LoopbackPort}. Is another instance running?", true);
            }

            _running = true;
            _snapshot = BuildSnapshot(request, false);
            return _snapshot;
        }

        public Task<LobbySnapshot> JoinAsync(JoinRequest request, CancellationToken cancellationToken)
        {
            throw new MultiplayerException(MultiplayerErrorCode.InvalidOperation, "Local mode has no remote players to join.");
        }

        public Task<LobbySnapshot> ReconnectAsync(ReconnectRecord record, CancellationToken cancellationToken)
        {
            throw new MultiplayerException(MultiplayerErrorCode.SessionEnded, "A local session cannot be rejoined.");
        }

        public Task<IReadOnlyList<SessionListing>> QuerySessionsAsync(SessionQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<SessionListing>>(new SessionListing[0]);
        }

        public Task SetReadyAsync(bool ready, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SetMatchStartedAsync(bool started, CancellationToken cancellationToken)
        {
            if (_snapshot != null && _snapshot.MatchStarted != started)
            {
                _snapshot = _snapshot.WithMatchStarted(started);
                LobbyChanged?.Invoke(_snapshot);
                MatchStateChanged?.Invoke(started);
            }

            return Task.CompletedTask;
        }

        public async Task LeaveAsync()
        {
            _leaving = true;
            await NetworkManagerLifecycle.ShutdownAsync(_networkManager, CancellationToken.None);
            _running = false;
            _snapshot = null;
            Unsubscribe();
        }

        public void Dispose()
        {
            Unsubscribe();
            if (_running && _networkManager != null && _networkManager.IsListening && !_networkManager.ShutdownInProgress)
            {
                _networkManager.Shutdown();
            }

            _running = false;
        }

        private void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            _subscribed = true;
            _networkManager.OnServerStopped += OnServerStopped;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _subscribed = false;
            if (_networkManager != null)
            {
                _networkManager.OnServerStopped -= OnServerStopped;
            }
        }

        private void OnServerStopped(bool wasHost)
        {
            if (_leaving || !_running)
            {
                return;
            }

            _running = false;
            SessionEnded?.Invoke(SessionEndReason.ConnectionLost, new MultiplayerError(MultiplayerErrorCode.ConnectionLost, "The local host stopped."));
        }

        private LobbySnapshot BuildSnapshot(HostRequest request, bool matchStarted)
        {
            LobbyPlayerInfo[] players = new LobbyPlayerInfo[request.LocalPlayerCount];
            for (int i = 0; i < players.Length; i++)
            {
                string name = i == 0 ? request.DisplayName : $"{request.DisplayName} {i + 1}";
                players[i] = new LobbyPlayerInfo($"{LocalPlayerKey}-{i}", name, request.PlatformTag, i == 0, true, true, request.LocalPlayerCount, i);
            }

            return new LobbySnapshot(SessionMode.Local, LocalPlayerKey, "Local co-op", string.Empty, players.Length, true, true, false,
                request.PlatformTag, matchStarted, players);
        }
    }
}
