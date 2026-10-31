using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Pretzel.Multiplayer.Tests
{
    public sealed class MultiplayerSessionServiceTests
    {
        private FakeSessionBackend _backend;
        private InMemoryReconnectStore _store;
        private MultiplayerSessionService _service;
        private List<ConnectionState> _states;
        private List<MultiplayerError> _errors;
        private List<TimeSpan> _delays;
        private DateTime _now;
        private int _factoryCalls;
        private int _maxAttempts;

        [SetUp]
        public void SetUp()
        {
            _backend = new FakeSessionBackend();
            _store = new InMemoryReconnectStore();
            _states = new List<ConnectionState>();
            _errors = new List<MultiplayerError>();
            _delays = new List<TimeSpan>();
            _now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            _factoryCalls = 0;
            _maxAttempts = 3;
            _service = CreateService();
        }

        [TearDown]
        public void TearDown()
        {
            _service.Dispose();
        }

        private MultiplayerSessionService CreateService()
        {
            MultiplayerSessionService service = new MultiplayerSessionService(
                new NetworkSettings(),
                mode =>
                {
                    _factoryCalls++;
                    _backend.Mode = mode;
                    return _backend;
                },
                _store,
                new ExponentialBackoffPolicy(_maxAttempts, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(4)),
                () => _now,
                (delay, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    _delays.Add(delay);
                    return Task.CompletedTask;
                });
            service.StateChanged += (previous, current) => _states.Add(current);
            service.ErrorRaised += error => _errors.Add(error);
            return service;
        }

        private static JoinRequest CodeJoin()
        {
            return new JoinRequest { Kind = JoinKind.Code, Code = "ABC123", DisplayName = "Tester" };
        }

        private async Task JoinAsClientAsync()
        {
            bool joined = await _service.JoinOnlineAsync(CodeJoin());
            Assert.IsTrue(joined);
            _states.Clear();
        }

        [Test]
        public async Task StartLocal_Success_ReachesLobbyWithoutRecordAsync()
        {
            _backend.HostResult = TestLobbies.Local(2);

            bool ok = await _service.StartLocalAsync(new HostRequest { LocalPlayerCount = 2 });

            Assert.IsTrue(ok);
            Assert.AreEqual(ConnectionState.InLobby, _service.State);
            Assert.AreEqual(new[] { ConnectionState.Connecting, ConnectionState.InLobby }, _states);
            Assert.AreEqual(2, _service.Lobby.Players.Count);
            Assert.IsNull(_store.Load(), "Local sessions are not remembered");
            Assert.AreEqual(SessionMode.Local, _backend.Mode);
        }

        [Test]
        public async Task HostOnline_Success_DoesNotPersistARecordAsync()
        {
            bool ok = await _service.HostOnlineAsync(new HostRequest { SessionName = "Room", MaxPlayers = 4 });

            Assert.IsTrue(ok);
            Assert.AreEqual(ConnectionState.InLobby, _service.State);
            Assert.IsNull(_store.Load(), "A host cannot rejoin its own session");
        }

        [Test]
        public async Task JoinOnline_Client_PersistsRejoinRecordAsync()
        {
            await JoinAsClientAsync();

            ReconnectRecord record = _store.Load();
            Assert.IsNotNull(record);
            Assert.AreEqual("session-1", record.SessionId);
            Assert.AreEqual(SessionRole.Client, record.Role);
            Assert.AreEqual(_now, record.SavedAtUtc);
            Assert.IsTrue(record.CanRejoin());
        }

        [Test]
        public async Task Connect_LobbyIsAvailableWhenStateChangesAsync()
        {
            LobbySnapshot seen = null;
            _service.StateChanged += (previous, current) =>
            {
                if (current == ConnectionState.InLobby)
                {
                    seen = _service.Lobby;
                }
            };

            await _service.JoinOnlineAsync(CodeJoin());

            Assert.IsNotNull(seen);
        }

        [Test]
        public async Task Connect_Failure_ReturnsToDisconnectedAndReportsTheErrorAsync()
        {
            _backend.ConnectException = new MultiplayerException(MultiplayerErrorCode.SessionFull, "full");

            bool ok = await _service.JoinOnlineAsync(CodeJoin());

            Assert.IsFalse(ok);
            Assert.AreEqual(ConnectionState.Disconnected, _service.State);
            Assert.AreEqual(MultiplayerErrorCode.SessionFull, _errors[0].Code);
            Assert.AreEqual(1, _backend.LeaveCalls);
            Assert.AreEqual(1, _backend.DisposeCalls);
            Assert.IsNull(_store.Load());
        }

        [Test]
        public async Task Connect_UnexpectedException_IsReportedAsUnknownAsync()
        {
            _backend.ConnectException = new InvalidOperationException("boom");

            bool ok = await _service.HostOnlineAsync(new HostRequest());

            Assert.IsFalse(ok);
            Assert.AreEqual(MultiplayerErrorCode.Unknown, _errors[0].Code);
            Assert.AreEqual("boom", _errors[0].Message);
        }

        [Test]
        public async Task Connect_Cancelled_ReturnsToDisconnectedWithoutAnErrorAsync()
        {
            _backend.ConnectGate = new TaskCompletionSource<bool>();
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                Task<bool> pending = _service.JoinOnlineAsync(CodeJoin(), cts.Token);
                Assert.AreEqual(ConnectionState.Connecting, _service.State);

                cts.Cancel();
                bool ok = await pending;

                Assert.IsFalse(ok);
            }

            Assert.AreEqual(ConnectionState.Disconnected, _service.State);
            Assert.IsEmpty(_errors);
            Assert.AreEqual(1, _backend.LeaveCalls);
        }

        [Test]
        public async Task CancelPending_WhileConnecting_CancelsAsync()
        {
            _backend.ConnectGate = new TaskCompletionSource<bool>();
            Task<bool> pending = _service.JoinOnlineAsync(CodeJoin());

            _service.CancelPending();

            Assert.IsFalse(await pending);
            Assert.AreEqual(ConnectionState.Disconnected, _service.State);
        }

        [Test]
        public async Task InvalidRequest_IsRejectedBeforeAnyBackendIsCreatedAsync()
        {
            bool ok = await _service.StartLocalAsync(new HostRequest { LocalPlayerCount = 99 });

            Assert.IsFalse(ok);
            Assert.AreEqual(MultiplayerErrorCode.InvalidRequest, _errors[0].Code);
            Assert.AreEqual(0, _factoryCalls);
            Assert.AreEqual(ConnectionState.Disconnected, _service.State);
        }

        [Test]
        public async Task SecondConnect_WhileConnected_IsRejectedAsync()
        {
            await JoinAsClientAsync();

            bool ok = await _service.JoinOnlineAsync(CodeJoin());

            Assert.IsFalse(ok);
            Assert.AreEqual(MultiplayerErrorCode.InvalidOperation, _errors[0].Code);
            Assert.AreEqual(ConnectionState.InLobby, _service.State);
        }

        [Test]
        public async Task Request_IsNormalized_NameAndPlatformTagAsync()
        {
            HostRequest request = new HostRequest { DisplayName = "   ", SessionName = "Room" };

            await _service.HostOnlineAsync(request);

            Assert.AreEqual("Player", request.DisplayName);
            Assert.AreEqual(CrossPlayRules.NormalizeTag(PlatformTags.Current), request.PlatformTag);
        }

        [Test]
        public async Task StartMatch_ByHostWithReadyGuest_MovesToPlayingAsync()
        {
            _backend.HostResult = TestLobbies.Online(true, false, true);
            await _service.HostOnlineAsync(new HostRequest { SessionName = "Room" });

            bool ok = await _service.StartMatchAsync();

            Assert.IsTrue(ok);
            Assert.AreEqual(ConnectionState.Playing, _service.State);
            Assert.IsTrue(_backend.LastMatchStarted.Value);
            Assert.IsTrue(_service.Lobby.MatchStarted);
        }

        [Test]
        public async Task StartMatch_WhileGuestNotReady_IsRefusedAsync()
        {
            _backend.HostResult = TestLobbies.Online(true, false, false);
            await _service.HostOnlineAsync(new HostRequest { SessionName = "Room" });

            bool ok = await _service.StartMatchAsync();

            Assert.IsFalse(ok);
            Assert.AreEqual(ConnectionState.InLobby, _service.State);
            Assert.AreEqual(MultiplayerErrorCode.InvalidOperation, _errors[0].Code);
            Assert.IsNull(_backend.LastMatchStarted);
        }

        [Test]
        public async Task StartMatch_ByClient_IsRefusedAsync()
        {
            await JoinAsClientAsync();

            Assert.IsFalse(await _service.StartMatchAsync());
            Assert.AreEqual(ConnectionState.InLobby, _service.State);
        }

        [Test]
        public async Task EndMatch_ByHost_ReturnsToLobbyAsync()
        {
            _backend.HostResult = TestLobbies.Online(true, false, true);
            await _service.HostOnlineAsync(new HostRequest { SessionName = "Room" });
            await _service.StartMatchAsync();

            bool ok = await _service.EndMatchAsync();

            Assert.IsTrue(ok);
            Assert.AreEqual(ConnectionState.InLobby, _service.State);
            Assert.IsFalse(_service.Lobby.MatchStarted);
        }

        [Test]
        public async Task SetReady_ForwardsToTheBackendAsync()
        {
            await JoinAsClientAsync();

            Assert.IsTrue(await _service.SetReadyAsync(true));

            Assert.IsTrue(_backend.LastReady.Value);
        }

        [Test]
        public async Task BackendMatchState_MovesAClientBetweenLobbyAndPlayingAsync()
        {
            await JoinAsClientAsync();

            _backend.RaiseMatchState(true);
            Assert.AreEqual(ConnectionState.Playing, _service.State);
            Assert.IsTrue(_service.Lobby.MatchStarted);

            _backend.RaiseMatchState(false);
            Assert.AreEqual(ConnectionState.InLobby, _service.State);
        }

        [Test]
        public async Task BackendLobbyChanged_IsForwardedWithTheNewSnapshotAsync()
        {
            await JoinAsClientAsync();
            LobbySnapshot raised = null;
            _service.LobbyChanged += snapshot => raised = snapshot;
            LobbySnapshot updated = TestLobbies.Online(false, false, true);

            _backend.RaiseLobbyChanged(updated);

            Assert.AreSame(updated, raised);
            Assert.AreSame(updated, _service.Lobby);
        }

        [Test]
        public async Task ConnectionLost_ReconnectSucceedsOnSecondAttemptAsync()
        {
            await JoinAsClientAsync();
            _backend.ReconnectScript.Enqueue(new MultiplayerException(MultiplayerErrorCode.ConnectionFailed, "still down", true));
            _backend.ReconnectScript.Enqueue(TestLobbies.Online(false));
            List<ReconnectProgress> progress = new List<ReconnectProgress>();
            _service.ReconnectProgressChanged += progress.Add;

            _backend.RaiseConnectionLost();

            Assert.AreEqual(new[] { ConnectionState.Reconnecting, ConnectionState.InLobby }, _states);
            Assert.AreEqual(2, _backend.ReconnectCalls);
            Assert.AreEqual(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2) }, _delays, "Exponential backoff between attempts");
            Assert.AreEqual(2, progress.Count);
            Assert.AreEqual(2, progress[1].Attempt);
            Assert.IsNotNull(_store.Load(), "The record survives a successful reconnect");
        }

        [Test]
        public async Task ConnectionLost_ReconnectingDuringAMatch_ReturnsToPlayingAsync()
        {
            await JoinAsClientAsync();
            _backend.RaiseMatchState(true);
            _states.Clear();
            _backend.ReconnectScript.Enqueue(TestLobbies.Online(false, true));

            _backend.RaiseConnectionLost();

            Assert.AreEqual(ConnectionState.Playing, _service.State);
        }

        [Test]
        public async Task ConnectionLost_AttemptsExhausted_ShowsTheRejoinPromptAsync()
        {
            await JoinAsClientAsync();

            _backend.RaiseConnectionLost();

            Assert.AreEqual(ConnectionState.ReconnectPrompt, _service.State);
            Assert.AreEqual(3, _backend.ReconnectCalls);
            Assert.AreEqual(SessionEndReason.ConnectionLost, _service.LastEndReason);
            Assert.IsNotNull(_store.Load(), "The record stays so the player can still rejoin");
            Assert.AreEqual(MultiplayerErrorCode.ConnectionLost, _errors[_errors.Count - 1].Code);
        }

        [Test]
        public async Task ConnectionLost_WithZeroAttempts_PromptsImmediatelyAsync()
        {
            _service.Dispose();
            _maxAttempts = 0;
            _service = CreateService();
            await JoinAsClientAsync();

            _backend.RaiseConnectionLost();

            Assert.AreEqual(ConnectionState.ReconnectPrompt, _service.State);
            Assert.AreEqual(0, _backend.ReconnectCalls);
        }

        [Test]
        public async Task Prompt_Rejoin_RunsThePolicyAgainAndSucceedsAsync()
        {
            await JoinAsClientAsync();
            _backend.RaiseConnectionLost();
            Assert.AreEqual(ConnectionState.ReconnectPrompt, _service.State);
            _backend.ReconnectScript.Enqueue(TestLobbies.Online(false));

            bool ok = await _service.RejoinAsync();

            Assert.IsTrue(ok);
            Assert.AreEqual(ConnectionState.InLobby, _service.State);
            Assert.AreEqual(4, _backend.ReconnectCalls);
        }

        [Test]
        public async Task Prompt_Rejoin_FailingAgainReturnsToThePromptAsync()
        {
            await JoinAsClientAsync();
            _backend.RaiseConnectionLost();

            bool ok = await _service.RejoinAsync();

            Assert.IsFalse(ok);
            Assert.AreEqual(ConnectionState.ReconnectPrompt, _service.State);
        }

        [Test]
        public async Task Prompt_ReturnToLobby_LeavesAndForgetsTheSessionAsync()
        {
            await JoinAsClientAsync();
            _backend.RaiseConnectionLost();

            await _service.LeaveAsync();

            Assert.AreEqual(ConnectionState.Disconnected, _service.State);
            Assert.IsNull(_store.Load());
            Assert.IsNull(_service.PendingRejoin);
            Assert.AreEqual(SessionEndReason.LeftByUser, _service.LastEndReason);
            Assert.AreEqual(1, _backend.LeaveCalls);
            Assert.AreEqual(1, _backend.DisposeCalls);
        }

        [Test]
        public async Task Leave_FromLobby_LeavesTheBackendAsync()
        {
            await JoinAsClientAsync();

            await _service.LeaveAsync();

            Assert.AreEqual(new[] { ConnectionState.Disconnecting, ConnectionState.Disconnected }, _states);
            Assert.AreEqual(1, _backend.LeaveCalls);
            Assert.IsNull(_store.Load());
            Assert.IsNull(_service.Lobby);
        }

        [Test]
        public async Task Reconnect_WhenKicked_EndsTheSessionWithoutPromptAsync()
        {
            await JoinAsClientAsync();
            _backend.ReconnectScript.Enqueue(new MultiplayerException(MultiplayerErrorCode.Kicked, "kicked"));

            _backend.RaiseConnectionLost();

            Assert.AreEqual(ConnectionState.Disconnected, _service.State);
            Assert.AreEqual(SessionEndReason.Kicked, _service.LastEndReason);
            Assert.AreEqual(1, _backend.ReconnectCalls, "A terminal error stops the retries");
            Assert.IsNull(_store.Load());
        }

        [Test]
        public async Task Reconnect_WhenSessionIsGone_OnlyReturnToLobbyRemainsAsync()
        {
            await JoinAsClientAsync();
            _backend.ReconnectScript.Enqueue(new MultiplayerException(MultiplayerErrorCode.SessionNotFound, "gone"));

            _backend.RaiseConnectionLost();

            Assert.AreEqual(ConnectionState.Disconnected, _service.State);
            Assert.AreEqual(SessionEndReason.HostLeft, _service.LastEndReason);
            Assert.IsNull(_store.Load());
            Assert.AreEqual(MultiplayerErrorCode.SessionNotFound, _errors[0].Code);
        }

        [Test]
        public async Task Reconnect_WhenSessionIsFull_EndsTheSessionAsync()
        {
            await JoinAsClientAsync();
            _backend.ReconnectScript.Enqueue(new MultiplayerException(MultiplayerErrorCode.SessionFull, "full"));

            _backend.RaiseConnectionLost();

            Assert.AreEqual(ConnectionState.Disconnected, _service.State);
            Assert.IsNull(_store.Load());
        }

        [Test]
        public async Task SessionEnded_HostLeft_ReturnsToDisconnectedAndForgetsAsync()
        {
            await JoinAsClientAsync();

            _backend.RaiseSessionEnded(SessionEndReason.HostLeft, MultiplayerErrorCode.SessionEnded);

            Assert.AreEqual(ConnectionState.Disconnected, _service.State);
            Assert.AreEqual(SessionEndReason.HostLeft, _service.LastEndReason);
            Assert.IsNull(_store.Load());
            Assert.AreEqual(MultiplayerErrorCode.SessionEnded, _errors[0].Code);
            Assert.AreEqual(1, _backend.DisposeCalls);
        }

        [Test]
        public async Task SessionEnded_Kicked_DoesNotOfferRejoinAsync()
        {
            await JoinAsClientAsync();

            _backend.RaiseSessionEnded(SessionEndReason.Kicked, MultiplayerErrorCode.Kicked);

            Assert.AreEqual(ConnectionState.Disconnected, _service.State);
            Assert.AreEqual(SessionEndReason.Kicked, _service.LastEndReason);
            Assert.IsFalse(_service.CheckForPendingRejoin());
        }

        [Test]
        public async Task ConnectionLost_AsHost_EndsTheSessionWithoutReconnectingAsync()
        {
            await _service.HostOnlineAsync(new HostRequest { SessionName = "Room" });

            _backend.RaiseConnectionLost();

            Assert.AreEqual(ConnectionState.Disconnected, _service.State);
            Assert.AreEqual(0, _backend.ReconnectCalls);
            Assert.AreEqual(SessionEndReason.ConnectionLost, _service.LastEndReason);
        }

        [Test]
        public async Task ConnectionLost_InLocalMode_EndsTheSessionAsync()
        {
            _backend.HostResult = TestLobbies.Local();
            await _service.StartLocalAsync(new HostRequest { LocalPlayerCount = 2 });

            _backend.RaiseSessionEnded(SessionEndReason.ConnectionLost, MultiplayerErrorCode.ConnectionLost);

            Assert.AreEqual(ConnectionState.Disconnected, _service.State);
        }

        [Test]
        public async Task CancelPending_DuringReconnectDelay_ReturnsToThePromptAsync()
        {
            _service.Dispose();
            TaskCompletionSource<bool> gate = new TaskCompletionSource<bool>();
            _service = new MultiplayerSessionService(
                new NetworkSettings(),
                mode => _backend,
                _store,
                new ExponentialBackoffPolicy(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(4)),
                () => _now,
                (delay, token) =>
                {
                    token.Register(() => gate.TrySetCanceled());
                    return gate.Task;
                });
            await JoinAsClientAsync();

            _backend.RaiseConnectionLost();
            Assert.AreEqual(ConnectionState.Reconnecting, _service.State);
            _service.CancelPending();
            await WaitUntilAsync(() => _service.State == ConnectionState.ReconnectPrompt);

            Assert.AreEqual(0, _backend.ReconnectCalls);
        }

        [Test]
        public async Task ApplicationResumed_WithDeadConnection_StartsReconnectAsync()
        {
            await JoinAsClientAsync();
            _backend.IsConnected = false;
            _backend.ReconnectScript.Enqueue(TestLobbies.Online(false));

            _service.NotifyApplicationResumed();

            Assert.AreEqual(new[] { ConnectionState.Reconnecting, ConnectionState.InLobby }, _states);
        }

        [Test]
        public async Task ApplicationResumed_WithLiveConnection_DoesNothingAsync()
        {
            await JoinAsClientAsync();

            _service.NotifyApplicationResumed();

            Assert.IsEmpty(_states);
        }

        [Test]
        public void CheckForPendingRejoin_WithFreshRecord_OffersThePrompt()
        {
            _store.Save(ReconnectRecord.Create("session-1", SessionRole.Client, SessionMode.Online, 0, 1, "Tester", true, _now.AddMinutes(-2)));

            bool offered = _service.CheckForPendingRejoin();

            Assert.IsTrue(offered);
            Assert.AreEqual(ConnectionState.ReconnectPrompt, _service.State);
            Assert.AreEqual("session-1", _service.PendingRejoin.SessionId);
        }

        [Test]
        public async Task StartupPrompt_Rejoin_CreatesTheBackendAndReconnectsAsync()
        {
            _store.Save(ReconnectRecord.Create("session-1", SessionRole.Client, SessionMode.Online, 0, 1, "Tester", true, _now.AddMinutes(-2)));
            _service.CheckForPendingRejoin();
            _backend.ReconnectScript.Enqueue(TestLobbies.Online(false));

            bool ok = await _service.RejoinAsync();

            Assert.IsTrue(ok);
            Assert.AreEqual(ConnectionState.InLobby, _service.State);
            Assert.AreEqual(1, _factoryCalls);
        }

        [Test]
        public void CheckForPendingRejoin_WithExpiredRecord_ClearsItAndStaysDisconnected()
        {
            _store.Save(ReconnectRecord.Create("session-1", SessionRole.Client, SessionMode.Online, 0, 1, "Tester", true, _now.AddHours(-5)));

            bool offered = _service.CheckForPendingRejoin();

            Assert.IsFalse(offered);
            Assert.AreEqual(ConnectionState.Disconnected, _service.State);
            Assert.IsNull(_store.Load());
        }

        [Test]
        public void CheckForPendingRejoin_WithHostRecord_IsIgnoredAndCleared()
        {
            _store.Save(ReconnectRecord.Create("session-1", SessionRole.Host, SessionMode.Online, 0, 1, "Tester", true, _now));

            Assert.IsFalse(_service.CheckForPendingRejoin());
            Assert.IsNull(_store.Load());
        }

        [Test]
        public void CheckForPendingRejoin_WithNoRecord_ReturnsFalse()
        {
            Assert.IsFalse(_service.CheckForPendingRejoin());
            Assert.AreEqual(ConnectionState.Disconnected, _service.State);
        }

        [Test]
        public async Task Rejoin_WithoutPrompt_IsRefusedAsync()
        {
            Assert.IsFalse(await _service.RejoinAsync());
            Assert.AreEqual(MultiplayerErrorCode.InvalidOperation, _errors[0].Code);
        }

        [Test]
        public async Task QuerySessions_FiltersByPlatformWhenCrossPlayIsOffAsync()
        {
            _backend.QueryResult = new[]
            {
                new SessionListing("a", "Windows room", 1, 4, false, false, "windows"),
                new SessionListing("b", "Phone room", 1, 4, false, false, "android"),
                new SessionListing("c", "Untagged room", 1, 4, false, false, string.Empty),
            };

            IReadOnlyList<SessionListing> restricted = await _service.QuerySessionsAsync(new SessionQuery { CrossPlay = false, PlatformTag = "windows" });
            IReadOnlyList<SessionListing> open = await _service.QuerySessionsAsync(new SessionQuery { CrossPlay = true, PlatformTag = "windows" });

            Assert.AreEqual(new[] { "a", "c" }, new[] { restricted[0].Id, restricted[1].Id });
            Assert.AreEqual(3, open.Count, "Cross-play never restricts");
            Assert.AreEqual(2, _backend.DisposeCalls, "A query backend is temporary");
        }

        [Test]
        public async Task Dispose_DuringASession_DisposesTheBackendOnceAsync()
        {
            await JoinAsClientAsync();

            _service.Dispose();
            _service.Dispose();

            Assert.AreEqual(1, _backend.DisposeCalls);
        }

        private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 2000)
        {
            System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
            while (!condition())
            {
                if (watch.ElapsedMilliseconds > timeoutMs)
                {
                    Assert.Fail("Timed out waiting for the condition.");
                }

                await Task.Yield();
            }
        }
    }
}
