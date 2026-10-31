using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Pretzel.Core;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using Ugs = Unity.Services.Multiplayer;

namespace Pretzel.Multiplayer
{
    /// <summary>
    /// Online sessions through Unity Multiplayer Services (Sessions) with Relay, driving the NetworkManager through the SDK's built-in Netcode for GameObjects integration.
    /// Anonymous Authentication keeps it platform-agnostic. Everything the SDK throws is mapped to <see cref="MultiplayerException"/>.
    /// Hosting has been run against a live project with Multiplayer Services 2.4.1; joining, quick-join and reconnect have not.
    /// </summary>
    public sealed class OnlineSessionBackend : ISessionBackend
    {
        public const string PlatformKey = "pretzel.platform";
        public const string CrossPlayKey = "pretzel.crossplay";
        public const string MatchKey = "pretzel.match";
        public const string PlayerNameKey = "pretzel.name";
        public const string PlayerReadyKey = "pretzel.ready";
        public const string PlayerLocalCountKey = "pretzel.local";

        private static readonly TimeSpan _quickJoinTimeout = TimeSpan.FromSeconds(10);

        private readonly NetworkManager _networkManager;
        private readonly NetworkSettings _settings;
        private readonly string _sessionType = "pretzel-" + Guid.NewGuid().ToString("N");
        private Ugs.ISession _session;
        private SessionRequest _request;
        private string _hostId;
        private string _lastDisconnectReason = string.Empty;
        private bool _matchStarted;
        private bool _connected;
        private bool _leaving;
        private bool _networkSubscribed;
        private bool _sessionSubscribed;

        public SessionMode Mode => SessionMode.Online;

        public bool IsConnected
        {
            get
            {
                if (_networkManager == null || !_networkManager.IsListening)
                {
                    return false;
                }

                return _networkManager.IsServer || _networkManager.IsConnectedClient;
            }
        }

        public event Action<LobbySnapshot> LobbyChanged;

        public event Action<bool> MatchStateChanged;

        public event Action<SessionEndReason, MultiplayerError> SessionEnded;

        public event Action<MultiplayerError> ConnectionLost;

        public OnlineSessionBackend(NetworkManager networkManager, NetworkSettings settings)
        {
            _networkManager = Guard.NotDestroyed(networkManager, nameof(networkManager));
            _settings = Guard.NotNull(settings, nameof(settings));
        }

        public async Task<LobbySnapshot> HostAsync(HostRequest request, CancellationToken cancellationToken)
        {
            _request = request;
            try
            {
                await EnsureSignedInAsync(cancellationToken);
                PrepareNetwork(request);
                Ugs.SessionOptions options = new Ugs.SessionOptions
                {
                    Type = _sessionType,
                    Name = request.SessionName.Trim(),
                    MaxPlayers = request.MaxPlayers,
                    IsPrivate = request.IsPrivate,
                    PlayerProperties = BuildPlayerProperties(request, true),
                    SessionProperties = BuildSessionProperties(request),
                };
                options = Ugs.SessionOptionsExtensions.WithRelayNetwork(options);
                Ugs.IHostSession host = await Ugs.MultiplayerService.Instance.CreateSessionAsync(options, cancellationToken);
                return OnJoined(host);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw Fail(exception);
            }
        }

        public async Task<LobbySnapshot> JoinAsync(JoinRequest request, CancellationToken cancellationToken)
        {
            _request = request;
            try
            {
                await EnsureSignedInAsync(cancellationToken);
                PrepareNetwork(request);
                Ugs.ISession session;
                switch (request.Kind)
                {
                    case JoinKind.Code:
                        session = await Ugs.MultiplayerService.Instance.JoinSessionByCodeAsync(request.Code.Trim(), BuildJoinOptions(request), cancellationToken);
                        break;
                    case JoinKind.SessionId:
                        session = await Ugs.MultiplayerService.Instance.JoinSessionByIdAsync(request.SessionId.Trim(), BuildJoinOptions(request), cancellationToken);
                        break;
                    default:
                        session = await QuickJoinAsync(request, cancellationToken);
                        break;
                }

                return OnJoined(session);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw Fail(exception);
            }
        }

        public async Task<LobbySnapshot> ReconnectAsync(ReconnectRecord record, CancellationToken cancellationToken)
        {
            Guard.NotNull(record, nameof(record));
            _request = new SessionRequest
            {
                DisplayName = record.DisplayName,
                LocalPlayerCount = record.LocalPlayerCount,
                CrossPlay = record.CrossPlay,
                PlatformTag = PlatformTags.Current,
            };

            try
            {
                await EnsureSignedInAsync(cancellationToken);
                _connected = false;
                DetachSession();
                if (!await NetworkManagerLifecycle.ShutdownAsync(_networkManager, cancellationToken))
                {
                    throw new MultiplayerException(MultiplayerErrorCode.ConnectionFailed, "The previous connection did not shut down.", true);
                }

                PrepareNetwork(_request);
                Ugs.ReconnectSessionOptions options = new Ugs.ReconnectSessionOptions { Type = _sessionType };
                Ugs.ISession session = await Ugs.MultiplayerService.Instance.ReconnectToSessionAsync(record.SessionId, options, cancellationToken);
                string previousHost = _hostId;
                AttachSession(session);
                if (!_networkManager.IsConnectedClient)
                {
                    // The SDK only rejoins the network when the session's host is still the one that created it.
                    bool hostChanged = !string.IsNullOrEmpty(previousHost) && !string.Equals(previousHost, session.Host, StringComparison.Ordinal);
                    throw hostChanged
                        ? new MultiplayerException(MultiplayerErrorCode.SessionEnded, "The host left the match.")
                        : new MultiplayerException(MultiplayerErrorCode.ConnectionFailed, "The session was found but the network connection could not be restored.", true);
                }

                _connected = true;
                _matchStarted = ReadMatchFlag();
                return BuildSnapshot();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw Fail(exception);
            }
        }

        public async Task<IReadOnlyList<SessionListing>> QuerySessionsAsync(SessionQuery query, CancellationToken cancellationToken)
        {
            try
            {
                await EnsureSignedInAsync(cancellationToken);
                Ugs.QuerySessionsOptions options = new Ugs.QuerySessionsOptions
                {
                    Count = Mathf.Clamp(query.Count, 1, 100),
                    FilterOptions = BuildFilters(query.CrossPlay, query.PlatformTag),
                };
                Ugs.QuerySessionsResults results = await Ugs.MultiplayerService.Instance.QuerySessionsAsync(options, cancellationToken);
                List<SessionListing> listings = new List<SessionListing>(results.Sessions.Count);
                for (int i = 0; i < results.Sessions.Count; i++)
                {
                    Ugs.ISessionInfo info = results.Sessions[i];
                    string platform = info.Properties != null && info.Properties.TryGetValue(PlatformKey, out Ugs.SessionProperty property) ? property.Value : string.Empty;
                    listings.Add(new SessionListing(info.Id, info.Name, info.MaxPlayers - info.AvailableSlots, info.MaxPlayers, info.HasPassword, info.IsLocked, platform));
                }

                return listings;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw UgsErrorMapper.Map(exception);
            }
        }

        public async Task SetReadyAsync(bool ready, CancellationToken cancellationToken)
        {
            RequireSession();
            try
            {
                Ugs.IPlayer player = _session.CurrentPlayer;
                if (player == null)
                {
                    throw new MultiplayerException(MultiplayerErrorCode.InvalidOperation, "You are not a member of the session.");
                }

                player.SetProperty(PlayerReadyKey, new Ugs.PlayerProperty(ready ? "1" : "0", Ugs.VisibilityPropertyOptions.Member));
                await _session.SaveCurrentPlayerDataAsync();
            }
            catch (Exception exception) when (!(exception is OperationCanceledException))
            {
                throw UgsErrorMapper.Map(exception);
            }
        }

        public async Task SetMatchStartedAsync(bool started, CancellationToken cancellationToken)
        {
            RequireSession();
            try
            {
                Ugs.IHostSession host = _session.AsHost();
                host.SetProperty(MatchKey, new Ugs.SessionProperty(started ? "1" : "0", Ugs.VisibilityPropertyOptions.Member));
                if (!_settings.AllowLateJoin)
                {
                    host.IsLocked = started;
                }

                await host.SavePropertiesAsync();
                OnSessionPropertiesChanged();
            }
            catch (Exception exception) when (!(exception is OperationCanceledException))
            {
                throw UgsErrorMapper.Map(exception);
            }
        }

        public async Task LeaveAsync()
        {
            _leaving = true;
            Ugs.ISession session = _session;
            DetachSession();
            try
            {
                if (session != null && session.State != Ugs.SessionState.Deleted)
                {
                    if (session.IsHost)
                    {
                        await session.AsHost().DeleteAsync();
                    }
                    else
                    {
                        await session.LeaveAsync();
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Pretzel.Multiplayer] Leaving the online session failed: {exception.Message}");
            }

            await NetworkManagerLifecycle.ShutdownAsync(_networkManager, CancellationToken.None);
            _connected = false;
            _session = null;
            UnsubscribeNetwork();
        }

        public void Dispose()
        {
            DetachSession();
            UnsubscribeNetwork();
            if (_connected && _networkManager != null && _networkManager.IsListening && !_networkManager.ShutdownInProgress)
            {
                _networkManager.Shutdown();
            }

            _connected = false;
        }

        private async Task EnsureSignedInAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    InitializationOptions options = new InitializationOptions();
                    if (!string.IsNullOrEmpty(_settings.AuthenticationProfile))
                    {
                        options = options.SetProfile(_settings.AuthenticationProfile);
                    }

                    await UnityServices.InitializeAsync(options);
                }

                while (UnityServices.State == ServicesInitializationState.Initializing)
                {
                    await Awaitable.NextFrameAsync(cancellationToken);
                }

                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    throw new MultiplayerException(MultiplayerErrorCode.ServicesUnavailable, "Unity Services could not be initialized. Link the project to a Unity project id.");
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (MultiplayerException)
            {
                throw;
            }
            catch (Exception exception)
            {
                if (exception is AuthenticationException)
                {
                    throw UgsErrorMapper.Map(exception);
                }

                throw new MultiplayerException(MultiplayerErrorCode.ServicesUnavailable, "Unity Services could not be initialized: " + exception.Message, true, exception);
            }
        }

        private void PrepareNetwork(SessionRequest request)
        {
            SubscribeNetwork();
            _lastDisconnectReason = string.Empty;
            _leaving = false;
            ConnectionPayload payload = new ConnectionPayload(AuthenticationService.Instance.PlayerId, request.DisplayName, request.PlatformTag, request.LocalPlayerCount);
            NetworkManagerLifecycle.SetConnectionData(_networkManager, payload, _settings);
        }

        private async Task<Ugs.ISession> QuickJoinAsync(JoinRequest request, CancellationToken cancellationToken)
        {
            Ugs.QuickJoinOptions quick = new Ugs.QuickJoinOptions
            {
                CreateSession = false,
                Timeout = _quickJoinTimeout,
                Filters = BuildFilters(request.CrossPlay, request.PlatformTag),
            };
            Ugs.SessionOptions options = new Ugs.SessionOptions
            {
                Type = _sessionType,
                MaxPlayers = _settings.MaxPlayers,
                PlayerProperties = BuildPlayerProperties(request, false),
            };
            options = Ugs.SessionOptionsExtensions.WithRelayNetwork(options);
            return await Ugs.MultiplayerService.Instance.MatchmakeSessionAsync(quick, options, cancellationToken);
        }

        private Ugs.JoinSessionOptions BuildJoinOptions(SessionRequest request)
        {
            return new Ugs.JoinSessionOptions
            {
                Type = _sessionType,
                PlayerProperties = BuildPlayerProperties(request, false),
            };
        }

        private static List<Ugs.FilterOption> BuildFilters(bool crossPlay, string localPlatformTag)
        {
            List<Ugs.FilterOption> filters = new List<Ugs.FilterOption>
            {
                new Ugs.FilterOption(Ugs.FilterField.AvailableSlots, "0", Ugs.FilterOperation.Greater),
            };
            string tag = CrossPlayRules.GetQueryPlatformTag(crossPlay, localPlatformTag);
            if (tag != null)
            {
                filters.Add(new Ugs.FilterOption(Ugs.FilterField.StringIndex1, tag, Ugs.FilterOperation.Equal));
            }

            return filters;
        }

        private static Dictionary<string, Ugs.PlayerProperty> BuildPlayerProperties(SessionRequest request, bool ready)
        {
            return new Dictionary<string, Ugs.PlayerProperty>(StringComparer.Ordinal)
            {
                { PlayerNameKey, new Ugs.PlayerProperty(request.DisplayName, Ugs.VisibilityPropertyOptions.Member) },
                { PlatformKey, new Ugs.PlayerProperty(request.PlatformTag, Ugs.VisibilityPropertyOptions.Member) },
                { PlayerReadyKey, new Ugs.PlayerProperty(ready ? "1" : "0", Ugs.VisibilityPropertyOptions.Member) },
                { PlayerLocalCountKey, new Ugs.PlayerProperty(request.LocalPlayerCount.ToString(CultureInfo.InvariantCulture), Ugs.VisibilityPropertyOptions.Member) },
            };
        }

        private static Dictionary<string, Ugs.SessionProperty> BuildSessionProperties(SessionRequest request)
        {
            return new Dictionary<string, Ugs.SessionProperty>(StringComparer.Ordinal)
            {
                // String index 1 makes the platform tag queryable.
                { PlatformKey, new Ugs.SessionProperty(request.PlatformTag, Ugs.VisibilityPropertyOptions.Public, Ugs.PropertyIndex.String1) },
                { CrossPlayKey, new Ugs.SessionProperty(request.CrossPlay ? "1" : "0", Ugs.VisibilityPropertyOptions.Public) },
                { MatchKey, new Ugs.SessionProperty("0", Ugs.VisibilityPropertyOptions.Member) },
            };
        }

        private LobbySnapshot OnJoined(Ugs.ISession session)
        {
            _hostId = session.Host;
            AttachSession(session);
            _connected = true;
            _matchStarted = ReadMatchFlag();
            return BuildSnapshot();
        }

        private void AttachSession(Ugs.ISession session)
        {
            DetachSession();
            _session = session;
            _session.Changed += OnSessionChanged;
            _session.PlayerPropertiesChanged += OnSessionChanged;
            _session.PlayerJoined += OnPlayerEvent;
            _session.PlayerHasLeft += OnPlayerEvent;
            _session.SessionPropertiesChanged += OnSessionPropertiesChanged;
            _session.SessionHostChanged += OnSessionHostChanged;
            _session.Deleted += OnSessionDeleted;
            _session.RemovedFromSession += OnRemovedFromSession;
            _sessionSubscribed = true;
        }

        private void DetachSession()
        {
            if (!_sessionSubscribed || _session == null)
            {
                return;
            }

            _session.Changed -= OnSessionChanged;
            _session.PlayerPropertiesChanged -= OnSessionChanged;
            _session.PlayerJoined -= OnPlayerEvent;
            _session.PlayerHasLeft -= OnPlayerEvent;
            _session.SessionPropertiesChanged -= OnSessionPropertiesChanged;
            _session.SessionHostChanged -= OnSessionHostChanged;
            _session.Deleted -= OnSessionDeleted;
            _session.RemovedFromSession -= OnRemovedFromSession;
            _sessionSubscribed = false;
        }

        private void SubscribeNetwork()
        {
            if (_networkSubscribed)
            {
                return;
            }

            _networkSubscribed = true;
            _networkManager.OnClientDisconnectCallback += OnClientDisconnect;
            _networkManager.OnClientStopped += OnClientStopped;
            _networkManager.OnServerStopped += OnServerStopped;
        }

        private void UnsubscribeNetwork()
        {
            if (!_networkSubscribed)
            {
                return;
            }

            _networkSubscribed = false;
            if (_networkManager != null)
            {
                _networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
                _networkManager.OnClientStopped -= OnClientStopped;
                _networkManager.OnServerStopped -= OnServerStopped;
            }
        }

        private void OnClientDisconnect(ulong clientId)
        {
            if (_networkManager != null && clientId == _networkManager.LocalClientId && !_networkManager.IsServer)
            {
                _lastDisconnectReason = _networkManager.DisconnectReason ?? string.Empty;
            }
        }

        private void OnClientStopped(bool wasHost)
        {
            if (wasHost || _leaving || !_connected)
            {
                return;
            }

            _connected = false;
            string detail = string.IsNullOrEmpty(_lastDisconnectReason) ? "The connection to the host was lost." : _lastDisconnectReason;
            ConnectionLost?.Invoke(new MultiplayerError(MultiplayerErrorCode.ConnectionLost, detail, true));
        }

        private void OnServerStopped(bool wasHost)
        {
            if (_leaving || !_connected || !wasHost)
            {
                return;
            }

            _connected = false;
            SessionEnded?.Invoke(SessionEndReason.ConnectionLost, new MultiplayerError(MultiplayerErrorCode.ConnectionLost, "The host connection stopped."));
        }

        private void OnSessionChanged()
        {
            if (_session != null && !_leaving)
            {
                LobbyChanged?.Invoke(BuildSnapshot());
            }
        }

        private void OnPlayerEvent(string playerId)
        {
            OnSessionChanged();
        }

        private void OnSessionPropertiesChanged()
        {
            if (_session == null || _leaving)
            {
                return;
            }

            bool started = ReadMatchFlag();
            if (started != _matchStarted)
            {
                _matchStarted = started;
                LobbyChanged?.Invoke(BuildSnapshot());
                MatchStateChanged?.Invoke(started);
            }
        }

        private void OnSessionHostChanged(string newHostId)
        {
            // Host migration is not enabled, so a new lobby host means the network host is gone.
            if (!_leaving && _session != null && !string.Equals(newHostId, _hostId, StringComparison.Ordinal))
            {
                SessionEnded?.Invoke(SessionEndReason.HostLeft, new MultiplayerError(MultiplayerErrorCode.SessionEnded, "The host left the match."));
            }
        }

        private void OnSessionDeleted()
        {
            if (!_leaving)
            {
                SessionEnded?.Invoke(SessionEndReason.HostLeft, new MultiplayerError(MultiplayerErrorCode.SessionEnded, "The host ended the session."));
            }
        }

        private void OnRemovedFromSession()
        {
            if (!_leaving)
            {
                SessionEnded?.Invoke(SessionEndReason.Kicked, new MultiplayerError(MultiplayerErrorCode.Kicked, "You were removed from the session."));
            }
        }

        private bool ReadMatchFlag()
        {
            return _session != null && _session.Properties != null && _session.Properties.TryGetValue(MatchKey, out Ugs.SessionProperty property)
                && string.Equals(property.Value, "1", StringComparison.Ordinal);
        }

        private LobbySnapshot BuildSnapshot()
        {
            Ugs.ISession session = _session;
            string localId = session.CurrentPlayer?.Id;
            List<LobbyPlayerInfo> players = new List<LobbyPlayerInfo>(session.Players.Count);
            for (int i = 0; i < session.Players.Count; i++)
            {
                Ugs.IReadOnlyPlayer player = session.Players[i];
                string name = ReadPlayer(player, PlayerNameKey);
                int localCount = 1;
                int.TryParse(ReadPlayer(player, PlayerLocalCountKey), NumberStyles.Integer, CultureInfo.InvariantCulture, out localCount);
                players.Add(new LobbyPlayerInfo(
                    player.Id,
                    string.IsNullOrEmpty(name) ? "Player" : name,
                    ReadPlayer(player, PlatformKey),
                    string.Equals(player.Id, session.Host, StringComparison.Ordinal),
                    string.Equals(player.Id, localId, StringComparison.Ordinal),
                    string.Equals(ReadPlayer(player, PlayerReadyKey), "1", StringComparison.Ordinal),
                    Mathf.Max(1, localCount)));
            }

            string sessionPlatform = ReadSession(PlatformKey);
            bool crossPlay = !string.Equals(ReadSession(CrossPlayKey), "0", StringComparison.Ordinal);
            return new LobbySnapshot(SessionMode.Online, session.Id, session.Name, session.Code, session.MaxPlayers, session.IsPrivate, session.IsHost,
                crossPlay, sessionPlatform, _matchStarted, players);
        }

        private string ReadSession(string key)
        {
            return _session.Properties != null && _session.Properties.TryGetValue(key, out Ugs.SessionProperty property) ? property.Value ?? string.Empty : string.Empty;
        }

        private static string ReadPlayer(Ugs.IReadOnlyPlayer player, string key)
        {
            return player.Properties != null && player.Properties.TryGetValue(key, out Ugs.PlayerProperty property) ? property.Value ?? string.Empty : string.Empty;
        }

        private void RequireSession()
        {
            if (_session == null)
            {
                throw new MultiplayerException(MultiplayerErrorCode.InvalidOperation, "There is no session.");
            }
        }

        private MultiplayerException Fail(Exception exception)
        {
            MultiplayerException mapped = UgsErrorMapper.Map(exception);
            if (!string.IsNullOrEmpty(_lastDisconnectReason) && mapped.Error.Code == MultiplayerErrorCode.ConnectionFailed)
            {
                // The host rejected the connection during approval, for example because the session is full.
                MultiplayerErrorCode code = UgsErrorMapper.Classify(Ugs.SessionError.Unknown, _lastDisconnectReason);
                return new MultiplayerException(code == MultiplayerErrorCode.Unknown ? MultiplayerErrorCode.ConnectionFailed : code, _lastDisconnectReason, true, exception);
            }

            return mapped;
        }
    }
}
