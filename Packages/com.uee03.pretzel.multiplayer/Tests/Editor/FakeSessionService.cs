using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pretzel.Multiplayer.Tests
{
    /// <summary>A hand-driven service for presenter tests: set the state and raise events, inspect the calls.</summary>
    public sealed class FakeSessionService : IMultiplayerSessionService
    {
        private readonly List<string> _calls = new List<string>();

        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

        public LobbySnapshot Lobby { get; set; }

        public ReconnectRecord PendingRejoin { get; set; }

        public SessionEndReason LastEndReason { get; set; }

        public IReadOnlyList<string> Calls => _calls;

        public HostRequest LastHostRequest { get; private set; }

        public JoinRequest LastJoinRequest { get; private set; }

        public SessionQuery LastQuery { get; private set; }

        public IReadOnlyList<SessionListing> Sessions { get; set; } = new SessionListing[0];

        public Exception ThrowOnStart { get; set; }

        public event Action<ConnectionState, ConnectionState> StateChanged;

        public event Action<LobbySnapshot> LobbyChanged;

        public event Action<MultiplayerError> ErrorRaised;

        public event Action<ReconnectProgress> ReconnectProgressChanged;

        public int StateSubscriberCount => StateChanged?.GetInvocationList().Length ?? 0;

        public void SetState(ConnectionState state)
        {
            ConnectionState previous = State;
            State = state;
            StateChanged?.Invoke(previous, state);
        }

        public void RaiseLobby(LobbySnapshot lobby)
        {
            Lobby = lobby;
            LobbyChanged?.Invoke(lobby);
        }

        public void RaiseError(MultiplayerError error)
        {
            ErrorRaised?.Invoke(error);
        }

        public void RaiseProgress(int attempt)
        {
            ReconnectProgressChanged?.Invoke(new ReconnectProgress(attempt, TimeSpan.Zero));
        }

        public Task<bool> StartLocalAsync(HostRequest request, CancellationToken cancellationToken = default)
        {
            _calls.Add("StartLocal");
            LastHostRequest = request;
            return ResultAsync();
        }

        public Task<bool> HostOnlineAsync(HostRequest request, CancellationToken cancellationToken = default)
        {
            _calls.Add("HostOnline");
            LastHostRequest = request;
            return ResultAsync();
        }

        public Task<bool> JoinOnlineAsync(JoinRequest request, CancellationToken cancellationToken = default)
        {
            _calls.Add("JoinOnline");
            LastJoinRequest = request;
            return ResultAsync();
        }

        public Task<IReadOnlyList<SessionListing>> QuerySessionsAsync(SessionQuery query, CancellationToken cancellationToken = default)
        {
            _calls.Add("Query");
            LastQuery = query;
            return Task.FromResult(Sessions);
        }

        public Task<bool> SetReadyAsync(bool ready, CancellationToken cancellationToken = default)
        {
            _calls.Add("Ready:" + ready);
            return ResultAsync();
        }

        public Task<bool> StartMatchAsync(CancellationToken cancellationToken = default)
        {
            _calls.Add("StartMatch");
            return ResultAsync();
        }

        public Task<bool> EndMatchAsync(CancellationToken cancellationToken = default)
        {
            _calls.Add("EndMatch");
            return ResultAsync();
        }

        public bool CheckForPendingRejoin()
        {
            _calls.Add("CheckForPendingRejoin");
            return false;
        }

        public Task<bool> RejoinAsync(CancellationToken cancellationToken = default)
        {
            _calls.Add("Rejoin");
            return ResultAsync();
        }

        public void CancelPending()
        {
            _calls.Add("CancelPending");
        }

        public Task LeaveAsync()
        {
            _calls.Add("Leave");
            return Task.CompletedTask;
        }

        public void NotifyApplicationResumed()
        {
            _calls.Add("Resumed");
        }

        public void Dispose()
        {
        }

        public void ClearCalls()
        {
            _calls.Clear();
        }

        private Task<bool> ResultAsync()
        {
            if (ThrowOnStart != null)
            {
                throw ThrowOnStart;
            }

            return Task.FromResult(true);
        }
    }
}
