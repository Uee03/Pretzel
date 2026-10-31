using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pretzel.Multiplayer.Tests
{
    /// <summary>A scriptable backend: tests decide what each call returns or throws and raise its events by hand.</summary>
    public sealed class FakeSessionBackend : ISessionBackend
    {
        /// <summary>The scripted outcomes of reconnect attempts, consumed in order.</summary>
        public sealed class ReconnectScriptQueue
        {
            private readonly Queue<object> _entries = new Queue<object>();

            public int Count => _entries.Count;

            public void Enqueue(object entry)
            {
                _entries.Enqueue(entry);
            }

            public object Dequeue()
            {
                return _entries.Dequeue();
            }
        }

        public SessionMode Mode { get; set; } = SessionMode.Online;

        public bool IsConnected { get; set; } = true;

        public LobbySnapshot HostResult { get; set; } = TestLobbies.Online(true);

        public LobbySnapshot JoinResult { get; set; } = TestLobbies.Online(false);

        public Exception ConnectException { get; set; }

        /// <summary>When set, Host/Join wait for it and then complete, so a test can cancel in between.</summary>
        public TaskCompletionSource<bool> ConnectGate { get; set; }

        /// <summary>Each reconnect attempt takes the next entry: a snapshot to succeed with, or an exception to fail with. An empty queue fails retryably.</summary>
        public ReconnectScriptQueue ReconnectScript { get; } = new ReconnectScriptQueue();

        public IReadOnlyList<SessionListing> QueryResult { get; set; } = new SessionListing[0];

        public int HostCalls { get; private set; }

        public int JoinCalls { get; private set; }

        public int ReconnectCalls { get; private set; }

        public int LeaveCalls { get; private set; }

        public int DisposeCalls { get; private set; }

        public bool? LastReady { get; private set; }

        public bool? LastMatchStarted { get; private set; }

        public event Action<LobbySnapshot> LobbyChanged;

        public event Action<bool> MatchStateChanged;

        public event Action<SessionEndReason, MultiplayerError> SessionEnded;

        public event Action<MultiplayerError> ConnectionLost;

        public async Task<LobbySnapshot> HostAsync(HostRequest request, CancellationToken cancellationToken)
        {
            HostCalls++;
            await WaitForGateAsync(cancellationToken);
            ThrowConfigured();
            return HostResult;
        }

        public async Task<LobbySnapshot> JoinAsync(JoinRequest request, CancellationToken cancellationToken)
        {
            JoinCalls++;
            await WaitForGateAsync(cancellationToken);
            ThrowConfigured();
            return JoinResult;
        }

        public Task<LobbySnapshot> ReconnectAsync(ReconnectRecord record, CancellationToken cancellationToken)
        {
            ReconnectCalls++;
            cancellationToken.ThrowIfCancellationRequested();
            object next = ReconnectScript.Count > 0 ? ReconnectScript.Dequeue() : new MultiplayerException(MultiplayerErrorCode.ConnectionFailed, "still down", true);
            if (next is Exception exception)
            {
                throw exception;
            }

            return Task.FromResult((LobbySnapshot)next);
        }

        public Task<IReadOnlyList<SessionListing>> QuerySessionsAsync(SessionQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult(QueryResult);
        }

        public Task SetReadyAsync(bool ready, CancellationToken cancellationToken)
        {
            LastReady = ready;
            return Task.CompletedTask;
        }

        public Task SetMatchStartedAsync(bool started, CancellationToken cancellationToken)
        {
            LastMatchStarted = started;
            return Task.CompletedTask;
        }

        public Task LeaveAsync()
        {
            LeaveCalls++;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            DisposeCalls++;
        }

        public void RaiseConnectionLost()
        {
            ConnectionLost?.Invoke(new MultiplayerError(MultiplayerErrorCode.ConnectionLost, "lost", true));
        }

        public void RaiseSessionEnded(SessionEndReason reason, MultiplayerErrorCode code)
        {
            SessionEnded?.Invoke(reason, new MultiplayerError(code, code.ToString()));
        }

        public void RaiseLobbyChanged(LobbySnapshot snapshot)
        {
            LobbyChanged?.Invoke(snapshot);
        }

        public void RaiseMatchState(bool started)
        {
            MatchStateChanged?.Invoke(started);
        }

        private async Task WaitForGateAsync(CancellationToken cancellationToken)
        {
            if (ConnectGate == null)
            {
                return;
            }

            using (cancellationToken.Register(() => ConnectGate.TrySetCanceled()))
            {
                await ConnectGate.Task;
            }
        }

        private void ThrowConfigured()
        {
            if (ConnectException != null)
            {
                throw ConnectException;
            }
        }
    }
}
