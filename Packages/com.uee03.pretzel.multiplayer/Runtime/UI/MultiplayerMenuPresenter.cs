using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Pretzel.Core;
using Pretzel.UI;

namespace Pretzel.Multiplayer.UI
{
    /// <summary>
    /// The logic of the multiplayer menu (MVP). It reacts to <see cref="IMultiplayerSessionService"/> and to view actions and drives an <see cref="IMultiplayerMenuView"/>;
    /// it never touches a VisualElement or a backend type, so it runs in plain EditMode tests with fakes.
    /// </summary>
    public sealed class MultiplayerMenuPresenter : IDisposable
    {
        private readonly IMultiplayerSessionService _service;
        private readonly IMultiplayerMenuView _view;
        private readonly string _platformTag;
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private bool _bound;
        private bool _disposed;

        public MultiplayerMenuPresenter(IMultiplayerSessionService service, IMultiplayerMenuView view, string platformTag = null)
        {
            _service = Guard.NotNull(service, nameof(service));
            _view = Guard.NotNull(view, nameof(view));
            _platformTag = CrossPlayRules.NormalizeTag(string.IsNullOrEmpty(platformTag) ? PlatformTags.Current : platformTag);
        }

        /// <summary>Subscribes to the view and the service and shows the screen for the current state. Call once.</summary>
        public void Bind(string defaultDisplayName = "Player")
        {
            if (_bound)
            {
                return;
            }

            _bound = true;
            _view.WriteForm(new MenuForm { DisplayName = defaultDisplayName, LocalPlayers = 2 });
            _view.SetCrossPlayHint(BuildCrossPlayHint(true));
            _view.ActionInvoked += OnAction;
            _view.SessionChosen += OnSessionChosen;
            _service.StateChanged += OnStateChanged;
            _service.LobbyChanged += OnLobbyChanged;
            _service.ErrorRaised += OnError;
            _service.ReconnectProgressChanged += OnReconnectProgress;
            Apply(_service.State);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _lifetime.Cancel();
            if (_bound)
            {
                _view.ActionInvoked -= OnAction;
                _view.SessionChosen -= OnSessionChosen;
                _service.StateChanged -= OnStateChanged;
                _service.LobbyChanged -= OnLobbyChanged;
                _service.ErrorRaised -= OnError;
                _service.ReconnectProgressChanged -= OnReconnectProgress;
            }

            _lifetime.Dispose();
        }

        private void OnAction(MenuAction action)
        {
            switch (action)
            {
                case MenuAction.PickLocal:
                    Run(ct => _service.StartLocalAsync(BuildHostRequest(true), ct));
                    break;
                case MenuAction.PickHost:
                    if (_service.State == ConnectionState.Disconnected)
                    {
                        _view.ShowScreen(MenuScreen.Host);
                    }

                    break;
                case MenuAction.PickJoin:
                    if (_service.State == ConnectionState.Disconnected)
                    {
                        _view.ShowScreen(MenuScreen.Join);
                        RefreshSessions();
                    }

                    break;
                case MenuAction.PickQuickJoin:
                    Run(ct => _service.JoinOnlineAsync(BuildJoinRequest(JoinKind.QuickJoin, null), ct));
                    break;
                case MenuAction.Back:
                    if (_service.State == ConnectionState.Disconnected)
                    {
                        _view.ShowScreen(MenuScreen.Mode);
                    }

                    break;
                case MenuAction.SubmitHost:
                    Run(ct => _service.HostOnlineAsync(BuildHostRequest(false), ct));
                    break;
                case MenuAction.SubmitJoin:
                    Run(ct => _service.JoinOnlineAsync(BuildJoinRequest(JoinKind.Code, null), ct));
                    break;
                case MenuAction.RefreshSessions:
                    RefreshSessions();
                    break;
                case MenuAction.ToggleReady:
                    bool next = !LobbyRules.IsLocalReady(_service.Lobby);
                    Run(ct => _service.SetReadyAsync(next, ct));
                    break;
                case MenuAction.StartMatch:
                    Run(ct => _service.StartMatchAsync(ct));
                    break;
                case MenuAction.Leave:
                case MenuAction.ReturnToLobby:
                    Run(ct => _service.LeaveAsync());
                    break;
                case MenuAction.Cancel:
                    _service.CancelPending();
                    break;
                case MenuAction.Rejoin:
                    Run(ct => _service.RejoinAsync(ct));
                    break;
                case MenuAction.CrossPlayChanged:
                    _view.SetCrossPlayHint(BuildCrossPlayHint(_view.ReadForm().CrossPlay));
                    break;
            }
        }

        private void OnSessionChosen(SessionListing listing)
        {
            if (listing == null || !listing.IsJoinable)
            {
                _view.ShowToast("That session is not joinable.", ToastKind.Warning);
                return;
            }

            Run(ct => _service.JoinOnlineAsync(BuildJoinRequest(JoinKind.SessionId, listing.Id), ct));
        }

        private void OnStateChanged(ConnectionState previous, ConnectionState current)
        {
            Apply(current);
        }

        private void OnLobbyChanged(LobbySnapshot lobby)
        {
            if (_service.State == ConnectionState.InLobby)
            {
                _view.SetLobby(BuildLobby(lobby));
            }
            else if (_service.State == ConnectionState.Playing)
            {
                _view.SetHud(BuildHud(lobby));
            }
        }

        private void OnError(MultiplayerError error)
        {
            ToastKind kind = ToastKind.Error;
            if (error.Code == MultiplayerErrorCode.Kicked || error.Code == MultiplayerErrorCode.SessionEnded || error.Code == MultiplayerErrorCode.ConnectionLost)
            {
                kind = ToastKind.Warning;
            }

            _view.ShowToast(error.Message, kind);
        }

        private void OnReconnectProgress(ReconnectProgress progress)
        {
            if (_service.State == ConnectionState.Reconnecting)
            {
                _view.SetBusy(string.Format(CultureInfo.InvariantCulture, "Reconnecting (attempt {0})...", progress.Attempt), true);
            }
        }

        private void Apply(ConnectionState state)
        {
            switch (state)
            {
                case ConnectionState.Disconnected:
                    _view.SetHud(null);
                    _view.ShowScreen(MenuScreen.Mode);
                    break;
                case ConnectionState.Connecting:
                    _view.SetHud(null);
                    _view.SetBusy("Connecting...", true);
                    break;
                case ConnectionState.InLobby:
                    _view.SetHud(null);
                    if (_service.Lobby != null)
                    {
                        _view.SetLobby(BuildLobby(_service.Lobby));
                    }

                    _view.ShowScreen(MenuScreen.Lobby);
                    break;
                case ConnectionState.Playing:
                    _view.SetHud(BuildHud(_service.Lobby));
                    _view.ShowScreen(MenuScreen.None);
                    break;
                case ConnectionState.Reconnecting:
                    _view.SetHud(null);
                    _view.SetBusy("Reconnecting...", true);
                    break;
                case ConnectionState.ReconnectPrompt:
                    _view.SetHud(null);
                    _view.SetRejoin(BuildRejoinMessage());
                    _view.ShowScreen(MenuScreen.Rejoin);
                    break;
                case ConnectionState.Disconnecting:
                    _view.SetHud(null);
                    _view.SetBusy("Leaving...", false);
                    break;
            }
        }

        private void RefreshSessions()
        {
            MenuForm form = _view.ReadForm();
            Run(async ct =>
            {
                IReadOnlyList<SessionListing> sessions = await _service.QuerySessionsAsync(new SessionQuery { CrossPlay = form.CrossPlay, PlatformTag = _platformTag }, ct);
                _view.SetSessions(sessions);
            });
        }

        private HostRequest BuildHostRequest(bool local)
        {
            MenuForm form = _view.ReadForm();
            return new HostRequest
            {
                DisplayName = form.DisplayName,
                LocalPlayerCount = local ? form.LocalPlayers : 1,
                CrossPlay = form.CrossPlay,
                PlatformTag = _platformTag,
                SessionName = form.SessionName,
                MaxPlayers = form.MaxPlayers,
                IsPrivate = form.IsPrivate,
            };
        }

        private JoinRequest BuildJoinRequest(JoinKind kind, string sessionId)
        {
            MenuForm form = _view.ReadForm();
            return new JoinRequest
            {
                DisplayName = form.DisplayName,
                LocalPlayerCount = 1,
                CrossPlay = form.CrossPlay,
                PlatformTag = _platformTag,
                Kind = kind,
                Code = form.JoinCode,
                SessionId = sessionId,
            };
        }

        // The single async-void boundary: view events are synchronous callbacks. Failures are shown as toasts, cancellation is silent.
        private async void Run(Func<CancellationToken, Task> operation)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                await operation(_lifetime.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception exception)
            {
                if (!_disposed)
                {
                    _view.ShowToast(MultiplayerError.FromException(exception).Message, ToastKind.Error);
                }
            }
        }

        internal static LobbyViewModel BuildLobby(LobbySnapshot lobby)
        {
            List<LobbyPlayerRow> rows = new List<LobbyPlayerRow>(lobby.Players.Count);
            for (int i = 0; i < lobby.Players.Count; i++)
            {
                LobbyPlayerInfo player = lobby.Players[i];
                string name = player.IsLocal && lobby.Mode == SessionMode.Online ? player.DisplayName + " (you)" : player.DisplayName;
                string status;
                if (lobby.Mode == SessionMode.Local)
                {
                    status = "Player " + (player.LocalPlayerIndex + 1).ToString(CultureInfo.InvariantCulture);
                }
                else if (player.IsHost)
                {
                    status = "Host";
                }
                else
                {
                    status = player.IsReady ? "Ready" : "Waiting";
                }

                rows.Add(new LobbyPlayerRow(name, player.PlatformTag, status, player.IsLocal, player.IsHost, player.IsReady || player.IsHost));
            }

            bool online = lobby.Mode == SessionMode.Online;
            string title = online ? lobby.SessionName : "Local co-op";
            string code = online && !string.IsNullOrEmpty(lobby.JoinCode) ? "Code: " + lobby.JoinCode : string.Empty;
            string info = online
                ? string.Format(CultureInfo.InvariantCulture, "{0}/{1} players - cross-play {2} - {3}", lobby.Players.Count, lobby.MaxPlayers, lobby.CrossPlay ? "on" : "off", lobby.PlatformTag)
                : string.Format(CultureInfo.InvariantCulture, "{0} local players on this device", lobby.Players.Count);
            return new LobbyViewModel(title, code, info, rows, online && !lobby.LocalIsHost, LobbyRules.IsLocalReady(lobby), lobby.LocalIsHost, LobbyRules.CanStart(lobby));
        }

        internal static string BuildHud(LobbySnapshot lobby)
        {
            if (lobby == null)
            {
                return "In match";
            }

            if (lobby.Mode == SessionMode.Local)
            {
                return string.Format(CultureInfo.InvariantCulture, "Local co-op - {0} players", lobby.Players.Count);
            }

            return string.Format(CultureInfo.InvariantCulture, "{0} - {1}/{2} players", lobby.SessionName, lobby.Players.Count, lobby.MaxPlayers);
        }

        internal string BuildRejoinMessage()
        {
            if (_service.LastEndReason == SessionEndReason.ConnectionLost)
            {
                return "The connection to the match was lost. Rejoin it, or return to the lobby to start a new game.";
            }

            return "You were in a match that is still running. Rejoin it, or return to the lobby to start a new game.";
        }

        internal string BuildCrossPlayHint(bool crossPlay)
        {
            return crossPlay
                ? "On: join anyone, on any platform."
                : "Off: browse and quick-join only " + _platformTag + " sessions.";
        }
    }
}
