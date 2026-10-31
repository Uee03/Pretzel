using System;
using System.Linq;
using NUnit.Framework;
using Pretzel.Multiplayer.UI;
using Pretzel.UI;

namespace Pretzel.Multiplayer.Tests
{
    public sealed class MultiplayerMenuPresenterTests
    {
        private FakeSessionService _service;
        private FakeMenuView _view;
        private MultiplayerMenuPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _service = new FakeSessionService();
            _view = new FakeMenuView();
            _presenter = new MultiplayerMenuPresenter(_service, _view, "windows");
            _presenter.Bind("Tester");
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Dispose();
        }

        [Test]
        public void Bind_WhenDisconnected_ShowsTheModePickerWithDefaults()
        {
            Assert.AreEqual(MenuScreen.Mode, _view.Screen);
            Assert.AreEqual("Tester", _view.Form.DisplayName);
            Assert.IsNull(_view.Hud);
            StringAssert.Contains("any platform", _view.CrossPlayHint);
        }

        [Test]
        public void Bind_WhenAlreadyInALobby_ShowsTheLobby()
        {
            FakeSessionService service = new FakeSessionService();
            service.SetState(ConnectionState.Connecting);
            service.SetState(ConnectionState.InLobby);
            service.Lobby = TestLobbies.Online(false);
            FakeMenuView view = new FakeMenuView();

            using (MultiplayerMenuPresenter presenter = new MultiplayerMenuPresenter(service, view, "windows"))
            {
                presenter.Bind();
            }

            Assert.AreEqual(MenuScreen.Lobby, view.Screen);
            Assert.AreEqual("Test session", view.Lobby.Title);
        }

        [Test]
        public void PickLocal_StartsALocalSessionWithTheFormValues()
        {
            _view.Form = new MenuForm { DisplayName = "Ada", LocalPlayers = 2, CrossPlay = false };

            _view.Click(MenuAction.PickLocal);

            Assert.AreEqual("StartLocal", _service.Calls.Single());
            Assert.AreEqual("Ada", _service.LastHostRequest.DisplayName);
            Assert.AreEqual(2, _service.LastHostRequest.LocalPlayerCount);
            Assert.IsFalse(_service.LastHostRequest.CrossPlay);
            Assert.AreEqual("windows", _service.LastHostRequest.PlatformTag);
        }

        [Test]
        public void PickHost_ShowsTheHostForm_AndSubmitCreatesTheSession()
        {
            _view.Form = new MenuForm { DisplayName = "Ada", SessionName = "Room", MaxPlayers = 3, IsPrivate = true, LocalPlayers = 2 };

            _view.Click(MenuAction.PickHost);
            Assert.AreEqual(MenuScreen.Host, _view.Screen);
            _view.Click(MenuAction.SubmitHost);

            Assert.AreEqual("HostOnline", _service.Calls.Single());
            Assert.AreEqual("Room", _service.LastHostRequest.SessionName);
            Assert.AreEqual(3, _service.LastHostRequest.MaxPlayers);
            Assert.IsTrue(_service.LastHostRequest.IsPrivate);
            Assert.AreEqual(1, _service.LastHostRequest.LocalPlayerCount, "Online hosts start with one local player");
        }

        [Test]
        public void SubmitJoin_JoinsByCode()
        {
            _view.Form = new MenuForm { JoinCode = "ABC123" };

            _view.Click(MenuAction.SubmitJoin);

            Assert.AreEqual(JoinKind.Code, _service.LastJoinRequest.Kind);
            Assert.AreEqual("ABC123", _service.LastJoinRequest.Code);
        }

        [Test]
        public void PickQuickJoin_UsesQuickJoin()
        {
            _view.Click(MenuAction.PickQuickJoin);

            Assert.AreEqual(JoinKind.QuickJoin, _service.LastJoinRequest.Kind);
        }

        [Test]
        public void PickJoin_ShowsTheBrowserAndQueriesSessions()
        {
            SessionListing listing = new SessionListing("id-1", "Room", 1, 4, false, false, "windows");
            _service.Sessions = new[] { listing };
            _view.Form = new MenuForm { CrossPlay = false };

            _view.Click(MenuAction.PickJoin);

            Assert.AreEqual(MenuScreen.Join, _view.Screen);
            Assert.AreEqual("Query", _service.Calls.Single());
            Assert.IsFalse(_service.LastQuery.CrossPlay);
            Assert.AreEqual("windows", _service.LastQuery.PlatformTag);
            Assert.AreSame(listing, _view.Sessions[0]);
        }

        [Test]
        public void ChoosingAJoinableSession_JoinsItById()
        {
            _view.Choose(new SessionListing("id-9", "Room", 1, 4, false, false, "windows"));

            Assert.AreEqual(JoinKind.SessionId, _service.LastJoinRequest.Kind);
            Assert.AreEqual("id-9", _service.LastJoinRequest.SessionId);
        }

        [Test]
        public void ChoosingAFullSession_ShowsAWarningInstead()
        {
            _view.Choose(new SessionListing("id-9", "Room", 4, 4, false, false, "windows"));

            Assert.IsEmpty(_service.Calls);
            Assert.AreEqual(ToastKind.Warning, _view.Toasts.Single().Value);
        }

        [Test]
        public void Back_ReturnsToTheModePicker_OnlyWhenDisconnected()
        {
            _view.Click(MenuAction.PickHost);
            _view.Click(MenuAction.Back);
            Assert.AreEqual(MenuScreen.Mode, _view.Screen);

            _service.SetState(ConnectionState.Connecting);
            _service.SetState(ConnectionState.InLobby);
            _service.Lobby = TestLobbies.Online(false);
            _view.Click(MenuAction.Back);
            Assert.AreEqual(MenuScreen.Lobby, _view.Screen);
        }

        [Test]
        public void Connecting_ShowsTheBusyScreen_AndCancelCancels()
        {
            _service.SetState(ConnectionState.Connecting);

            Assert.AreEqual(MenuScreen.Busy, _view.Screen);
            Assert.IsTrue(_view.BusyCanCancel);

            _view.Click(MenuAction.Cancel);
            Assert.AreEqual("CancelPending", _service.Calls.Single());
        }

        [Test]
        public void InLobby_AsHost_ShowsStartThatEnablesWhenGuestsAreReady()
        {
            _service.SetState(ConnectionState.Connecting);
            _service.Lobby = TestLobbies.Online(true, false, false);
            _service.SetState(ConnectionState.InLobby);

            Assert.AreEqual(MenuScreen.Lobby, _view.Screen);
            Assert.IsTrue(_view.Lobby.ShowStart);
            Assert.IsFalse(_view.Lobby.CanStart);
            Assert.IsFalse(_view.Lobby.ShowReady, "A host is always ready");

            _service.RaiseLobby(TestLobbies.Online(true, false, true));

            Assert.IsTrue(_view.Lobby.CanStart);
        }

        [Test]
        public void InLobby_AsClient_ShowsReadyAndToggles()
        {
            _service.SetState(ConnectionState.Connecting);
            _service.Lobby = TestLobbies.Online(false, false, false);
            _service.SetState(ConnectionState.InLobby);

            Assert.IsTrue(_view.Lobby.ShowReady);
            Assert.IsFalse(_view.Lobby.ShowStart);

            _view.Click(MenuAction.ToggleReady);
            Assert.AreEqual("Ready:True", _service.Calls.Single());

            _service.RaiseLobby(TestLobbies.Online(false, false, true));
            _view.Click(MenuAction.ToggleReady);
            Assert.AreEqual("Ready:False", _service.Calls.Last());
        }

        [Test]
        public void Lobby_ListsPlayersWithPlatformAndStatus()
        {
            _service.SetState(ConnectionState.Connecting);
            _service.Lobby = TestLobbies.Online(false, false, true);
            _service.SetState(ConnectionState.InLobby);

            LobbyViewModel lobby = _view.Lobby;

            Assert.AreEqual("Code: ABC123", lobby.Code);
            Assert.AreEqual(2, lobby.Players.Count);
            Assert.AreEqual("Host", lobby.Players[0].Name);
            Assert.AreEqual("windows", lobby.Players[0].Platform);
            Assert.AreEqual("Host", lobby.Players[0].Status);
            Assert.AreEqual("Guest (you)", lobby.Players[1].Name);
            Assert.AreEqual("android", lobby.Players[1].Platform);
            Assert.AreEqual("Ready", lobby.Players[1].Status);
            StringAssert.Contains("cross-play on", lobby.Info);
        }

        [Test]
        public void LocalLobby_ListsLocalPlayers_WithoutACode()
        {
            _service.SetState(ConnectionState.Connecting);
            _service.Lobby = TestLobbies.Local(2);
            _service.SetState(ConnectionState.InLobby);

            Assert.AreEqual(string.Empty, _view.Lobby.Code);
            Assert.AreEqual("Player 2", _view.Lobby.Players[1].Status);
            Assert.IsTrue(_view.Lobby.CanStart);
            Assert.IsFalse(_view.Lobby.ShowReady);
        }

        [Test]
        public void StartAndLeaveActions_CallTheService()
        {
            _view.Click(MenuAction.StartMatch);
            _view.Click(MenuAction.Leave);

            Assert.AreEqual(new[] { "StartMatch", "Leave" }, _service.Calls);
        }

        [Test]
        public void Playing_HidesTheMenuAndShowsTheOverlay()
        {
            _service.SetState(ConnectionState.Connecting);
            _service.Lobby = TestLobbies.Online(true, true, true);
            _service.SetState(ConnectionState.Playing);

            Assert.AreEqual(MenuScreen.None, _view.Screen);
            StringAssert.Contains("Test session", _view.Hud);
        }

        [Test]
        public void Reconnecting_ShowsProgress()
        {
            _service.SetState(ConnectionState.Connecting);
            _service.SetState(ConnectionState.InLobby);
            _service.SetState(ConnectionState.Reconnecting);

            _service.RaiseProgress(2);

            Assert.AreEqual(MenuScreen.Busy, _view.Screen);
            StringAssert.Contains("attempt 2", _view.BusyMessage);
            Assert.IsTrue(_view.BusyCanCancel);
        }

        [Test]
        public void ReconnectPrompt_ShowsTheRejoinDialog_WithBothChoices()
        {
            _service.LastEndReason = SessionEndReason.ConnectionLost;
            _service.SetState(ConnectionState.ReconnectPrompt);

            Assert.AreEqual(MenuScreen.Rejoin, _view.Screen);
            StringAssert.Contains("connection", _view.RejoinMessage);

            _view.Click(MenuAction.Rejoin);
            _view.Click(MenuAction.ReturnToLobby);

            Assert.AreEqual(new[] { "Rejoin", "Leave" }, _service.Calls);
        }

        [Test]
        public void StartupPrompt_UsesTheStillRunningMessage()
        {
            _service.LastEndReason = SessionEndReason.None;
            _service.SetState(ConnectionState.ReconnectPrompt);

            StringAssert.Contains("still running", _view.RejoinMessage);
        }

        [Test]
        public void Errors_BecomeToasts_WithTheRightKind()
        {
            _service.RaiseError(new MultiplayerError(MultiplayerErrorCode.SessionFull, "Full"));
            _service.RaiseError(new MultiplayerError(MultiplayerErrorCode.Kicked, "Kicked"));
            _service.RaiseError(new MultiplayerError(MultiplayerErrorCode.SessionEnded, "Ended"));

            Assert.AreEqual(ToastKind.Error, _view.Toasts[0].Value);
            Assert.AreEqual(ToastKind.Warning, _view.Toasts[1].Value);
            Assert.AreEqual(ToastKind.Warning, _view.Toasts[2].Value);
            Assert.AreEqual("Full", _view.Toasts[0].Key);
        }

        [Test]
        public void ThrowingOperations_AreShownAsAToast_NotPropagated()
        {
            _service.ThrowOnStart = new InvalidOperationException("kaboom");

            Assert.DoesNotThrow(() => _view.Click(MenuAction.PickQuickJoin));

            Assert.AreEqual("kaboom", _view.Toasts.Single().Key);
            Assert.AreEqual(ToastKind.Error, _view.Toasts.Single().Value);
        }

        [Test]
        public void CrossPlayToggle_UpdatesTheHint()
        {
            _view.Form = new MenuForm { CrossPlay = false };

            _view.Click(MenuAction.CrossPlayChanged);

            StringAssert.Contains("only windows", _view.CrossPlayHint);
        }

        [Test]
        public void Disconnecting_ShowsABusyScreenWithoutCancel()
        {
            _service.SetState(ConnectionState.Connecting);
            _service.SetState(ConnectionState.InLobby);
            _service.SetState(ConnectionState.Disconnecting);

            Assert.AreEqual(MenuScreen.Busy, _view.Screen);
            Assert.IsFalse(_view.BusyCanCancel);
        }

        [Test]
        public void Dispose_UnsubscribesFromTheServiceAndTheView()
        {
            _presenter.Dispose();
            _service.ClearCalls();

            _service.SetState(ConnectionState.Connecting);
            _view.Click(MenuAction.StartMatch);
            _service.RaiseError(new MultiplayerError(MultiplayerErrorCode.Unknown, "late"));

            Assert.AreEqual(0, _service.StateSubscriberCount);
            Assert.IsEmpty(_service.Calls);
            Assert.IsEmpty(_view.Toasts);
        }
    }
}
