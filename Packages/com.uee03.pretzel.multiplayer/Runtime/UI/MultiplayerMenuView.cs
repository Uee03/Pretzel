using System;
using System.Collections.Generic;
using System.Globalization;
using Pretzel.Core;
using Pretzel.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Multiplayer.UI
{
    /// <summary>
    /// The UI Toolkit side of the menu: finds the elements of MultiplayerMenu.uxml once, turns clicks into <see cref="MenuAction"/>s and renders what the presenter says.
    /// Screens use <see cref="UIScreenStack"/>, errors use <see cref="ToastService"/>, lists are virtualized ListViews.
    /// </summary>
    public sealed class MultiplayerMenuView : IMultiplayerMenuView, IDisposable
    {
        private const float RowHeight = 34f;

        private readonly VisualElement _root;
        private readonly bool _animate;
        private readonly UIScreenStack _screens;
        private readonly ToastService _toasts;
        private readonly Dictionary<MenuScreen, VisualElement> _screenElements = new Dictionary<MenuScreen, VisualElement>();
        private readonly List<IDisposable> _bindings = new List<IDisposable>();
        private readonly List<LobbyPlayerRow> _playerRows = new List<LobbyPlayerRow>();
        private readonly List<SessionListing> _sessionRows = new List<SessionListing>();

        private readonly TextField _displayName;
        private readonly SliderInt _localPlayers;
        private readonly Toggle _crossPlay;
        private readonly Label _crossPlayHint;
        private readonly TextField _sessionName;
        private readonly SliderInt _maxPlayers;
        private readonly Toggle _privateSession;
        private readonly TextField _joinCode;
        private readonly ListView _sessionList;
        private readonly Label _sessionListEmpty;
        private readonly Label _lobbyTitle;
        private readonly Label _lobbyCode;
        private readonly Label _lobbyInfo;
        private readonly ListView _playerList;
        private readonly Button _readyButton;
        private readonly Button _startButton;
        private readonly Label _busyMessage;
        private readonly ProgressBar _busyProgress;
        private readonly Button _busyCancel;
        private readonly Label _rejoinMessage;
        private readonly VisualElement _hud;
        private readonly Label _hudStatus;
        private readonly IVisualElementScheduledItem _progressItem;
        private float _progress;
        private bool _progressForward = true;

        public event Action<MenuAction> ActionInvoked;

        public event Action<SessionListing> SessionChosen;

        public MultiplayerMenuView(VisualElement root, bool animate = true)
        {
            _root = Guard.NotNull(root, nameof(root));
            _animate = animate;
            VisualElement block = root.QRequired<VisualElement>("multiplayer-root");
            block.pickingMode = PickingMode.Ignore;
            root.QRequired<VisualElement>("safe-area").pickingMode = PickingMode.Ignore;

            _screenElements[MenuScreen.Mode] = root.QRequired<VisualElement>("screen-mode");
            _screenElements[MenuScreen.Host] = root.QRequired<VisualElement>("screen-host");
            _screenElements[MenuScreen.Join] = root.QRequired<VisualElement>("screen-join");
            _screenElements[MenuScreen.Lobby] = root.QRequired<VisualElement>("screen-lobby");
            _screenElements[MenuScreen.Busy] = root.QRequired<VisualElement>("screen-busy");
            _screenElements[MenuScreen.Rejoin] = root.QRequired<VisualElement>("screen-rejoin");
            _screens = new UIScreenStack(animate ? 0.15f : 0f);
            _toasts = new ToastService(root.QRequired<VisualElement>("toasts"));

            _displayName = root.QRequired<TextField>("display-name");
            _localPlayers = root.QRequired<SliderInt>("local-players");
            _crossPlay = root.QRequired<Toggle>("cross-play");
            _crossPlayHint = root.QRequired<Label>("cross-play-hint");
            _sessionName = root.QRequired<TextField>("session-name");
            _maxPlayers = root.QRequired<SliderInt>("max-players");
            _privateSession = root.QRequired<Toggle>("private-session");
            _joinCode = root.QRequired<TextField>("join-code");
            _sessionList = root.QRequired<ListView>("session-list");
            _sessionListEmpty = root.QRequired<Label>("session-list-empty");
            _lobbyTitle = root.QRequired<Label>("lobby-title");
            _lobbyCode = root.QRequired<Label>("lobby-code");
            _lobbyInfo = root.QRequired<Label>("lobby-info");
            _playerList = root.QRequired<ListView>("player-list");
            _readyButton = root.QRequired<Button>("lobby-ready");
            _startButton = root.QRequired<Button>("lobby-start");
            _busyMessage = root.QRequired<Label>("busy-message");
            _busyProgress = root.QRequired<ProgressBar>("busy-progress");
            _busyCancel = root.QRequired<Button>("busy-cancel");
            _rejoinMessage = root.QRequired<Label>("rejoin-message");
            _hud = root.QRequired<VisualElement>("hud");
            _hudStatus = root.QRequired<Label>("hud-status");

            ConfigureLists();
            BindButton("mode-local", MenuAction.PickLocal);
            BindButton("mode-host", MenuAction.PickHost);
            BindButton("mode-join", MenuAction.PickJoin);
            BindButton("mode-quick", MenuAction.PickQuickJoin);
            BindButton("host-create", MenuAction.SubmitHost);
            BindButton("host-back", MenuAction.Back);
            BindButton("join-submit", MenuAction.SubmitJoin);
            BindButton("join-refresh", MenuAction.RefreshSessions);
            BindButton("join-back", MenuAction.Back);
            BindButton("lobby-ready", MenuAction.ToggleReady);
            BindButton("lobby-start", MenuAction.StartMatch);
            BindButton("lobby-leave", MenuAction.Leave);
            BindButton("busy-cancel", MenuAction.Cancel);
            BindButton("rejoin-accept", MenuAction.Rejoin);
            BindButton("rejoin-decline", MenuAction.ReturnToLobby);
            BindButton("hud-leave", MenuAction.Leave);
            _bindings.Add(_crossPlay.OnValueChanged((ChangeEvent<bool> evt) => ActionInvoked?.Invoke(MenuAction.CrossPlayChanged)));
            _bindings.Add(root.RegisterDisposable<NavigationCancelEvent>(evt => ActionInvoked?.Invoke(MenuAction.Back)));

            _busyProgress.lowValue = 0f;
            _busyProgress.highValue = 100f;
            _progressItem = _busyProgress.schedule.Execute(TickProgress).Every(50);
            _progressItem.Pause();
            _hud.SetDisplayed(false);
        }

        public MenuForm ReadForm()
        {
            return new MenuForm
            {
                DisplayName = _displayName.value,
                CrossPlay = _crossPlay.value,
                LocalPlayers = _localPlayers.value,
                SessionName = _sessionName.value,
                MaxPlayers = _maxPlayers.value,
                IsPrivate = _privateSession.value,
                JoinCode = _joinCode.value,
            };
        }

        public void WriteForm(MenuForm form)
        {
            _displayName.SetValueWithoutNotify(form.DisplayName);
            _crossPlay.SetValueWithoutNotify(form.CrossPlay);
            _localPlayers.SetValueWithoutNotify(form.LocalPlayers);
            _sessionName.SetValueWithoutNotify(form.SessionName);
            _maxPlayers.SetValueWithoutNotify(form.MaxPlayers);
            _privateSession.SetValueWithoutNotify(form.IsPrivate);
            _joinCode.SetValueWithoutNotify(form.JoinCode);
        }

        public void SetCrossPlayHint(string text)
        {
            _crossPlayHint.text = text;
        }

        public void ShowScreen(MenuScreen screen)
        {
            if (screen != MenuScreen.Busy)
            {
                _progressItem.Pause();
            }

            if (screen == MenuScreen.None)
            {
                _screens.Clear(_animate);
                return;
            }

            VisualElement target = _screenElements[screen];
            if (_screens.Current == target)
            {
                return;
            }

            if (_screens.Count == 0)
            {
                _screens.Push(target, false, _animate);
            }
            else
            {
                _screens.Replace(target, _animate);
            }
        }

        public void SetBusy(string message, bool canCancel)
        {
            _busyMessage.text = message;
            _busyCancel.SetDisplayed(canCancel);
            ShowScreen(MenuScreen.Busy);
            _progressItem.Resume();
        }

        public void SetLobby(LobbyViewModel lobby)
        {
            _lobbyTitle.text = lobby.Title;
            _lobbyCode.text = lobby.Code;
            _lobbyCode.SetDisplayed(!string.IsNullOrEmpty(lobby.Code));
            _lobbyInfo.text = lobby.Info;
            _playerRows.Clear();
            for (int i = 0; i < lobby.Players.Count; i++)
            {
                _playerRows.Add(lobby.Players[i]);
            }

            _playerList.RefreshItems();
            _readyButton.SetDisplayed(lobby.ShowReady);
            _readyButton.text = lobby.LocalIsReady ? "Not ready" : "Ready";
            _readyButton.EnableInClassList("pretzel-multiplayer__button--active", lobby.LocalIsReady);
            _startButton.SetDisplayed(lobby.ShowStart);
            _startButton.SetEnabled(lobby.CanStart);
        }

        public void SetSessions(IReadOnlyList<SessionListing> sessions)
        {
            _sessionRows.Clear();
            if (sessions != null)
            {
                for (int i = 0; i < sessions.Count; i++)
                {
                    _sessionRows.Add(sessions[i]);
                }
            }

            _sessionList.RefreshItems();
            _sessionListEmpty.SetDisplayed(_sessionRows.Count == 0);
        }

        public void SetRejoin(string message)
        {
            _rejoinMessage.text = message;
        }

        public void SetHud(string status)
        {
            _hud.SetDisplayed(status != null);
            if (status != null)
            {
                _hudStatus.text = status;
            }
        }

        public void ShowToast(string message, ToastKind kind)
        {
            _toasts.Show(message, 3.5f, kind);
        }

        public void Dispose()
        {
            _progressItem.Pause();
            for (int i = 0; i < _bindings.Count; i++)
            {
                _bindings[i].Dispose();
            }

            _bindings.Clear();
            _toasts.Clear();
            _screens.Clear(false);
            _playerList.makeItem = null;
            _playerList.bindItem = null;
            _sessionList.makeItem = null;
            _sessionList.bindItem = null;
        }

        private void ConfigureLists()
        {
            _playerList.itemsSource = _playerRows;
            _playerList.fixedItemHeight = RowHeight;
            _playerList.selectionType = SelectionType.None;
            _playerList.makeItem = MakePlayerRow;
            _playerList.bindItem = BindPlayerRow;
            _sessionList.itemsSource = _sessionRows;
            _sessionList.fixedItemHeight = RowHeight;
            _sessionList.selectionType = SelectionType.None;
            _sessionList.makeItem = MakeSessionRow;
            _sessionList.bindItem = BindSessionRow;
            _sessionListEmpty.SetDisplayed(true);
        }

        private void BindButton(string name, MenuAction action)
        {
            _bindings.Add(_root.QRequired<Button>(name).OnClick(() => ActionInvoked?.Invoke(action)));
        }

        private static VisualElement MakePlayerRow()
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("pretzel-multiplayer__item");
            row.Add(CreateLabel("item-name", "pretzel-multiplayer__item-name"));
            row.Add(CreateLabel("item-platform", "pretzel-multiplayer__item-detail"));
            row.Add(CreateLabel("item-status", "pretzel-multiplayer__item-status"));
            return row;
        }

        private void BindPlayerRow(VisualElement element, int index)
        {
            LobbyPlayerRow data = _playerRows[index];
            element.Q<Label>("item-name").text = data.Name;
            element.Q<Label>("item-platform").text = data.Platform;
            element.Q<Label>("item-status").text = data.Status;
            element.EnableInClassList("pretzel-multiplayer__item--local", data.IsLocal);
            element.EnableInClassList("pretzel-multiplayer__item--host", data.IsHost);
            element.EnableInClassList("pretzel-multiplayer__item--ready", data.IsReady);
        }

        private VisualElement MakeSessionRow()
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("pretzel-multiplayer__item");
            row.Add(CreateLabel("item-name", "pretzel-multiplayer__item-name"));
            row.Add(CreateLabel("item-count", "pretzel-multiplayer__item-detail"));
            row.Add(CreateLabel("item-platform", "pretzel-multiplayer__item-detail"));
            Button join = new Button { name = "item-join", text = "Join" };
            join.AddToClassList("pretzel-multiplayer__button");
            join.AddToClassList("pretzel-multiplayer__button--small");
            // One handler per recycled row: it reads the listing bound to the row at click time, so rebinding never stacks callbacks.
            join.clicked += () => SessionChosen?.Invoke(row.userData as SessionListing);
            row.Add(join);
            return row;
        }

        private void BindSessionRow(VisualElement element, int index)
        {
            SessionListing data = _sessionRows[index];
            element.userData = data;
            element.Q<Label>("item-name").text = data.HasPassword ? data.Name + " (password)" : data.Name;
            element.Q<Label>("item-count").text = string.Format(CultureInfo.InvariantCulture, "{0}/{1}", data.PlayerCount, data.MaxPlayers);
            element.Q<Label>("item-platform").text = data.PlatformTag;
            element.Q<Button>("item-join").SetEnabled(data.IsJoinable);
        }

        private static Label CreateLabel(string name, string className)
        {
            Label label = new Label { name = name };
            label.AddToClassList(className);
            return label;
        }

        private void TickProgress()
        {
            _progress += _progressForward ? 6f : -6f;
            if (_progress >= 100f)
            {
                _progress = 100f;
                _progressForward = false;
            }
            else if (_progress <= 0f)
            {
                _progress = 0f;
                _progressForward = true;
            }

            _busyProgress.value = _progress;
        }
    }
}
