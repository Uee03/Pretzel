using System;
using System.Collections.Generic;
using Pretzel.Multiplayer.UI;
using Pretzel.UI;

namespace Pretzel.Multiplayer.Tests
{
    /// <summary>Records what the presenter tells the view to do, and lets tests click.</summary>
    public sealed class FakeMenuView : IMultiplayerMenuView
    {
        private readonly List<KeyValuePair<string, ToastKind>> _toasts = new List<KeyValuePair<string, ToastKind>>();

        public MenuForm Form { get; set; } = new MenuForm { DisplayName = "Tester", LocalPlayers = 2, SessionName = "Room", MaxPlayers = 3, JoinCode = "XYZ" };

        public MenuScreen Screen { get; private set; } = MenuScreen.None;

        public string BusyMessage { get; private set; }

        public bool BusyCanCancel { get; private set; }

        public LobbyViewModel Lobby { get; private set; }

        public IReadOnlyList<SessionListing> Sessions { get; private set; }

        public string RejoinMessage { get; private set; }

        public string Hud { get; private set; } = "unset";

        public string CrossPlayHint { get; private set; }

        public IReadOnlyList<KeyValuePair<string, ToastKind>> Toasts => _toasts;

        public event Action<MenuAction> ActionInvoked;

        public event Action<SessionListing> SessionChosen;

        public void Click(MenuAction action)
        {
            ActionInvoked?.Invoke(action);
        }

        public void Choose(SessionListing listing)
        {
            SessionChosen?.Invoke(listing);
        }

        public MenuForm ReadForm()
        {
            return Form;
        }

        public void WriteForm(MenuForm form)
        {
            Form = form;
        }

        public void SetCrossPlayHint(string text)
        {
            CrossPlayHint = text;
        }

        public void ShowScreen(MenuScreen screen)
        {
            Screen = screen;
        }

        public void SetBusy(string message, bool canCancel)
        {
            BusyMessage = message;
            BusyCanCancel = canCancel;
            Screen = MenuScreen.Busy;
        }

        public void SetLobby(LobbyViewModel lobby)
        {
            Lobby = lobby;
        }

        public void SetSessions(IReadOnlyList<SessionListing> sessions)
        {
            Sessions = sessions;
        }

        public void SetRejoin(string message)
        {
            RejoinMessage = message;
        }

        public void SetHud(string status)
        {
            Hud = status;
        }

        public void ShowToast(string message, ToastKind kind)
        {
            _toasts.Add(new KeyValuePair<string, ToastKind>(message, kind));
        }
    }
}
