using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pretzel.Core;
using UnityEngine;

namespace Pretzel.Multiplayer
{
    /// <summary>
    /// The state machine around an <see cref="ISessionBackend"/>: connect, lobby, match, bounded reconnect with backoff, and the rejoin prompt.
    /// All calls and events happen on the main thread. It owns the backend for the lifetime of a session.
    /// </summary>
    public sealed class MultiplayerSessionService : IMultiplayerSessionService
    {
        private readonly NetworkSettings _settings;
        private readonly Func<SessionMode, ISessionBackend> _backendFactory;
        private readonly IReconnectStore _store;
        private readonly IReconnectPolicy _policy;
        private readonly Func<DateTime> _utcNow;
        private readonly Func<TimeSpan, CancellationToken, Task> _delay;
        private readonly ConnectionStateMachine _machine = new ConnectionStateMachine();
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();

        private ISessionBackend _backend;
        private LobbySnapshot _lobby;
        private ReconnectRecord _record;
        private SessionEndReason _lastEnd;
        private CancellationTokenSource _pending;
        private Task _reconnectTask = Task.CompletedTask;
        private bool _disposed;

        public ConnectionState State => _machine.State;

        public LobbySnapshot Lobby => _lobby;

        public ReconnectRecord PendingRejoin => _record;

        public SessionEndReason LastEndReason => _lastEnd;

        public event Action<ConnectionState, ConnectionState> StateChanged;

        public event Action<LobbySnapshot> LobbyChanged;

        public event Action<MultiplayerError> ErrorRaised;

        public event Action<ReconnectProgress> ReconnectProgressChanged;

        /// <param name="backendFactory">Creates the backend for a mode. Called once per session, and for session queries.</param>
        /// <param name="policy">Defaults to <see cref="NetworkSettings.CreateReconnectPolicy"/>.</param>
        /// <param name="utcNow">Clock, injectable for tests.</param>
        /// <param name="delay">Waits between reconnect attempts. Defaults to an Awaitable wait that also works on WebGL.</param>
        public MultiplayerSessionService(NetworkSettings settings, Func<SessionMode, ISessionBackend> backendFactory, IReconnectStore store,
            IReconnectPolicy policy = null, Func<DateTime> utcNow = null, Func<TimeSpan, CancellationToken, Task> delay = null)
        {
            _settings = Guard.NotNull(settings, nameof(settings));
            _backendFactory = Guard.NotNull(backendFactory, nameof(backendFactory));
            _store = Guard.NotNull(store, nameof(store));
            _policy = policy ?? settings.CreateReconnectPolicy();
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
            _delay = delay ?? DefaultDelayAsync;
            _machine.Changed += OnMachineChanged;
        }

        public Task<bool> StartLocalAsync(HostRequest request, CancellationToken cancellationToken = default)
        {
            MultiplayerError invalid = RequestValidator.ValidateHost(SessionMode.Local, request, _settings);
            return ConnectAsync(SessionMode.Local, request, invalid, SessionRole.Host, (backend, token) => backend.HostAsync(request, token), cancellationToken);
        }

        public Task<bool> HostOnlineAsync(HostRequest request, CancellationToken cancellationToken = default)
        {
            MultiplayerError invalid = RequestValidator.ValidateHost(SessionMode.Online, request, _settings);
            return ConnectAsync(SessionMode.Online, request, invalid, SessionRole.Host, (backend, token) => backend.HostAsync(request, token), cancellationToken);
        }

        public Task<bool> JoinOnlineAsync(JoinRequest request, CancellationToken cancellationToken = default)
        {
            MultiplayerError invalid = RequestValidator.ValidateJoin(request, _settings);
            return ConnectAsync(SessionMode.Online, request, invalid, SessionRole.Client, (backend, token) => backend.JoinAsync(request, token), cancellationToken);
        }

        public async Task<IReadOnlyList<SessionListing>> QuerySessionsAsync(SessionQuery query, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            SessionQuery effective = query ?? new SessionQuery();
            effective.PlatformTag = CrossPlayRules.NormalizeTag(string.IsNullOrWhiteSpace(effective.PlatformTag) ? PlatformTags.Current : effective.PlatformTag);
            bool temporary = _backend == null || _backend.Mode != SessionMode.Online;
            ISessionBackend backend = temporary ? _backendFactory(SessionMode.Online) : _backend;
            try
            {
                IReadOnlyList<SessionListing> raw = await backend.QuerySessionsAsync(effective, cancellationToken);
                List<SessionListing> visible = new List<SessionListing>(raw.Count);
                for (int i = 0; i < raw.Count; i++)
                {
                    if (CrossPlayRules.IsVisible(effective.CrossPlay, effective.PlatformTag, raw[i].PlatformTag))
                    {
                        visible.Add(raw[i]);
                    }
                }

                return visible;
            }
            catch (OperationCanceledException)
            {
                return new SessionListing[0];
            }
            catch (Exception exception)
            {
                Raise(MultiplayerError.FromException(exception));
                return new SessionListing[0];
            }
            finally
            {
                if (temporary)
                {
                    backend.Dispose();
                }
            }
        }

        public Task<bool> SetReadyAsync(bool ready, CancellationToken cancellationToken = default)
        {
            return RunSessionOperationAsync(ConnectionState.InLobby, false, backend => backend.SetReadyAsync(ready, cancellationToken));
        }

        public async Task<bool> StartMatchAsync(CancellationToken cancellationToken = default)
        {
            if (_lobby == null || !LobbyRules.CanStart(_lobby))
            {
                Raise(new MultiplayerError(MultiplayerErrorCode.InvalidOperation, "The match cannot start yet. Everyone has to be ready."));
                return false;
            }

            bool ok = await RunSessionOperationAsync(ConnectionState.InLobby, true, backend => backend.SetMatchStartedAsync(true, cancellationToken));
            if (ok)
            {
                OnBackendMatchStateChanged(true);
            }

            return ok;
        }

        public async Task<bool> EndMatchAsync(CancellationToken cancellationToken = default)
        {
            bool ok = await RunSessionOperationAsync(ConnectionState.Playing, true, backend => backend.SetMatchStartedAsync(false, cancellationToken));
            if (ok)
            {
                OnBackendMatchStateChanged(false);
            }

            return ok;
        }

        public bool CheckForPendingRejoin()
        {
            ThrowIfDisposed();
            if (State != ConnectionState.Disconnected)
            {
                return false;
            }

            ReconnectRecord stored = _store.Load();
            if (stored == null)
            {
                return false;
            }

            if (!stored.CanRejoin() || stored.IsExpired(_utcNow(), _settings.ReconnectRecordMaxAge))
            {
                _store.Clear();
                return false;
            }

            _record = stored;
            return _machine.TryTransition(ConnectionState.ReconnectPrompt);
        }

        public async Task<bool> RejoinAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (State != ConnectionState.ReconnectPrompt || _record == null)
            {
                Raise(new MultiplayerError(MultiplayerErrorCode.InvalidOperation, "There is no match to rejoin."));
                return false;
            }

            _reconnectTask = ReconnectLoopAsync(cancellationToken);
            await _reconnectTask;
            return State == ConnectionState.InLobby || State == ConnectionState.Playing;
        }

        public void CancelPending()
        {
            _pending?.Cancel();
        }

        public async Task LeaveAsync()
        {
            ThrowIfDisposed();
            ConnectionState state = State;
            if (state == ConnectionState.Disconnected)
            {
                ClearRecord();
                return;
            }

            if (state == ConnectionState.Disconnecting)
            {
                return;
            }

            CancelPending();
            if (state == ConnectionState.Connecting)
            {
                return;
            }

            await _reconnectTask;
            if (!_machine.TryTransition(ConnectionState.Disconnecting))
            {
                return;
            }

            ClearRecord();
            await TeardownBackendAsync(true);
            _lobby = null;
            _lastEnd = SessionEndReason.LeftByUser;
            _machine.TryTransition(ConnectionState.Disconnected);
        }

        public void NotifyApplicationResumed()
        {
            if (_disposed || _backend == null || (State != ConnectionState.InLobby && State != ConnectionState.Playing))
            {
                return;
            }

            if (!_backend.IsConnected)
            {
                HandleConnectionLost(new MultiplayerError(MultiplayerErrorCode.ConnectionLost, "The connection was lost while the app was suspended.", true));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _lifetime.Cancel();
            _pending?.Cancel();
            DetachBackend();
            _backend?.Dispose();
            _backend = null;
            _machine.Changed -= OnMachineChanged;
            _lifetime.Dispose();
        }

        private async Task<bool> ConnectAsync(SessionMode mode, SessionRequest request, MultiplayerError invalid, SessionRole role,
            Func<ISessionBackend, CancellationToken, Task<LobbySnapshot>> connect, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (State != ConnectionState.Disconnected)
            {
                Raise(new MultiplayerError(MultiplayerErrorCode.InvalidOperation, "Leave the current session first."));
                return false;
            }

            if (invalid != null)
            {
                Raise(invalid);
                return false;
            }

            NormalizeRequest(request);
            _lastEnd = SessionEndReason.None;
            _machine.Transition(ConnectionState.Connecting);
            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
            _pending = cts;
            try
            {
                _backend = _backendFactory(mode);
                AttachBackend();
                LobbySnapshot snapshot = await connect(_backend, cts.Token);
                cts.Token.ThrowIfCancellationRequested();
                OnConnected(snapshot, mode, role, request);
                return true;
            }
            catch (OperationCanceledException)
            {
                await AbortConnectAsync(null);
                return false;
            }
            catch (Exception exception)
            {
                await AbortConnectAsync(MultiplayerError.FromException(exception));
                return false;
            }
            finally
            {
                if (ReferenceEquals(_pending, cts))
                {
                    _pending = null;
                }

                cts.Dispose();
            }
        }

        private void OnConnected(LobbySnapshot snapshot, SessionMode mode, SessionRole role, SessionRequest request)
        {
            _lobby = snapshot;
            if (mode == SessionMode.Online && role == SessionRole.Client && !string.IsNullOrEmpty(snapshot.SessionId))
            {
                _record = ReconnectRecord.Create(snapshot.SessionId, role, mode, 0, request.LocalPlayerCount, request.DisplayName, request.CrossPlay, _utcNow());
                _store.Save(_record);
            }
            else
            {
                ClearRecord();
            }

            LobbyChanged?.Invoke(snapshot);
            _machine.Transition(snapshot.MatchStarted ? ConnectionState.Playing : ConnectionState.InLobby);
        }

        private async Task AbortConnectAsync(MultiplayerError error)
        {
            if (_disposed)
            {
                return;
            }

            await TeardownBackendAsync(true);
            _lobby = null;
            _machine.TryTransition(ConnectionState.Disconnected);
            if (error != null)
            {
                Raise(error);
            }
        }

        private async Task ReconnectLoopAsync(CancellationToken external)
        {
            if (!_machine.TryTransition(ConnectionState.Reconnecting))
            {
                return;
            }

            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(external, _lifetime.Token);
            _pending = cts;
            try
            {
                if (_backend == null)
                {
                    _backend = _backendFactory(SessionMode.Online);
                    AttachBackend();
                }

                int attempt = 1;
                while (true)
                {
                    TimeSpan? delay = _policy.GetDelay(attempt);
                    if (!delay.HasValue)
                    {
                        break;
                    }

                    ReconnectProgressChanged?.Invoke(new ReconnectProgress(attempt, delay.Value));
                    await _delay(delay.Value, cts.Token);
                    try
                    {
                        LobbySnapshot snapshot = await _backend.ReconnectAsync(_record, cts.Token);
                        OnReconnected(snapshot);
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (MultiplayerException exception) when (IsTerminal(exception.Error.Code))
                    {
                        HandleSessionEnded(ReasonFor(exception.Error.Code), exception.Error);
                        return;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning($"[Pretzel.Multiplayer] Reconnect attempt {attempt} failed: {exception.Message}");
                    }

                    attempt++;
                }

                if (State == ConnectionState.Reconnecting)
                {
                    _machine.TryTransition(ConnectionState.ReconnectPrompt);
                    Raise(new MultiplayerError(MultiplayerErrorCode.ConnectionLost, "Could not reconnect to the match.", true));
                }
            }
            catch (OperationCanceledException)
            {
                if (!_disposed && State == ConnectionState.Reconnecting)
                {
                    _machine.TryTransition(ConnectionState.ReconnectPrompt);
                }
            }
            finally
            {
                if (ReferenceEquals(_pending, cts))
                {
                    _pending = null;
                }

                cts.Dispose();
            }
        }

        private void OnReconnected(LobbySnapshot snapshot)
        {
            _lobby = snapshot;
            if (_record != null)
            {
                _record = ReconnectRecord.Create(_record.SessionId, _record.Role, _record.Mode, _record.LocalPlayerIndex, _record.LocalPlayerCount,
                    _record.DisplayName, _record.CrossPlay, _utcNow());
                _store.Save(_record);
            }

            LobbyChanged?.Invoke(snapshot);
            _machine.TryTransition(snapshot.MatchStarted ? ConnectionState.Playing : ConnectionState.InLobby);
        }

        private async Task<bool> RunSessionOperationAsync(ConnectionState required, bool hostOnly, Func<ISessionBackend, Task> operation)
        {
            ThrowIfDisposed();
            if (State != required || _backend == null)
            {
                Raise(new MultiplayerError(MultiplayerErrorCode.InvalidOperation, "That is not possible right now."));
                return false;
            }

            if (hostOnly && (_lobby == null || !_lobby.LocalIsHost))
            {
                Raise(new MultiplayerError(MultiplayerErrorCode.NotAuthorized, "Only the host can do that."));
                return false;
            }

            try
            {
                await operation(_backend);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception exception)
            {
                Raise(MultiplayerError.FromException(exception));
                return false;
            }
        }

        private void AttachBackend()
        {
            _backend.LobbyChanged += OnBackendLobbyChanged;
            _backend.MatchStateChanged += OnBackendMatchStateChanged;
            _backend.SessionEnded += HandleSessionEnded;
            _backend.ConnectionLost += HandleConnectionLost;
        }

        private void DetachBackend()
        {
            if (_backend == null)
            {
                return;
            }

            _backend.LobbyChanged -= OnBackendLobbyChanged;
            _backend.MatchStateChanged -= OnBackendMatchStateChanged;
            _backend.SessionEnded -= HandleSessionEnded;
            _backend.ConnectionLost -= HandleConnectionLost;
        }

        private async Task TeardownBackendAsync(bool leave)
        {
            ISessionBackend backend = _backend;
            if (backend == null)
            {
                return;
            }

            DetachBackend();
            _backend = null;
            try
            {
                if (leave)
                {
                    await backend.LeaveAsync();
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Pretzel.Multiplayer] Leaving the session failed: {exception.Message}");
            }
            finally
            {
                backend.Dispose();
            }
        }

        private void OnBackendLobbyChanged(LobbySnapshot snapshot)
        {
            _lobby = snapshot;
            LobbyChanged?.Invoke(snapshot);
        }

        private void OnBackendMatchStateChanged(bool started)
        {
            if (_lobby != null && _lobby.MatchStarted != started)
            {
                _lobby = _lobby.WithMatchStarted(started);
                LobbyChanged?.Invoke(_lobby);
            }

            if (started && State == ConnectionState.InLobby)
            {
                _machine.TryTransition(ConnectionState.Playing);
            }
            else if (!started && State == ConnectionState.Playing)
            {
                _machine.TryTransition(ConnectionState.InLobby);
            }
        }

        private void HandleSessionEnded(SessionEndReason reason, MultiplayerError error)
        {
            // While connecting, failures surface as exceptions from the connect call.
            if (_disposed || State == ConnectionState.Disconnected || State == ConnectionState.Disconnecting || State == ConnectionState.Connecting)
            {
                return;
            }

            _lastEnd = reason;
            ClearRecord();
            DetachBackend();
            _backend?.Dispose();
            _backend = null;
            _lobby = null;
            CancelPending();
            _machine.TryTransition(ConnectionState.Disconnected);
            if (error != null)
            {
                Raise(error);
            }
        }

        private void HandleConnectionLost(MultiplayerError error)
        {
            if (_disposed || (State != ConnectionState.InLobby && State != ConnectionState.Playing))
            {
                return;
            }

            if (_record == null || !_record.CanRejoin())
            {
                HandleSessionEnded(SessionEndReason.ConnectionLost, error ?? new MultiplayerError(MultiplayerErrorCode.ConnectionLost, "The connection was lost."));
                return;
            }

            _lastEnd = SessionEndReason.ConnectionLost;
            _reconnectTask = ReconnectLoopAsync(CancellationToken.None);
        }

        private void NormalizeRequest(SessionRequest request)
        {
            request.DisplayName = RequestValidator.NormalizeName(request.DisplayName);
            request.PlatformTag = CrossPlayRules.NormalizeTag(string.IsNullOrWhiteSpace(request.PlatformTag) ? PlatformTags.Current : request.PlatformTag);
        }

        private void ClearRecord()
        {
            _record = null;
            _store.Clear();
        }

        private void Raise(MultiplayerError error)
        {
            ErrorRaised?.Invoke(error);
        }

        private void OnMachineChanged(ConnectionState previous, ConnectionState current)
        {
            StateChanged?.Invoke(previous, current);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MultiplayerSessionService));
            }
        }

        private static bool IsTerminal(MultiplayerErrorCode code)
        {
            switch (code)
            {
                case MultiplayerErrorCode.SessionNotFound:
                case MultiplayerErrorCode.SessionEnded:
                case MultiplayerErrorCode.SessionFull:
                case MultiplayerErrorCode.Kicked:
                case MultiplayerErrorCode.NotAuthorized:
                case MultiplayerErrorCode.VersionMismatch:
                case MultiplayerErrorCode.InvalidRequest:
                    return true;
                default:
                    return false;
            }
        }

        private static SessionEndReason ReasonFor(MultiplayerErrorCode code)
        {
            switch (code)
            {
                case MultiplayerErrorCode.Kicked:
                    return SessionEndReason.Kicked;
                case MultiplayerErrorCode.SessionFull:
                case MultiplayerErrorCode.NotAuthorized:
                case MultiplayerErrorCode.VersionMismatch:
                case MultiplayerErrorCode.InvalidRequest:
                    return SessionEndReason.ConnectionLost;
                default:
                    return SessionEndReason.HostLeft;
            }
        }

        private static async Task DefaultDelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            await Awaitable.WaitForSecondsAsync((float)delay.TotalSeconds, cancellationToken);
        }
    }
}
