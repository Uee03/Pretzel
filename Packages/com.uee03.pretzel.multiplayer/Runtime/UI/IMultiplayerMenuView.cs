using System;
using System.Collections.Generic;
using Pretzel.UI;

namespace Pretzel.Multiplayer.UI
{
    /// <summary>The surface the presenter drives. Implemented by the UI Toolkit view and by fakes in tests. It holds no logic.</summary>
    public interface IMultiplayerMenuView
    {
        event Action<MenuAction> ActionInvoked;

        /// <summary>The player chose a session in the browser.</summary>
        event Action<SessionListing> SessionChosen;

        MenuForm ReadForm();

        void WriteForm(MenuForm form);

        void SetCrossPlayHint(string text);

        void ShowScreen(MenuScreen screen);

        /// <summary>Shows the busy screen with a message. <paramref name="canCancel"/> shows the Cancel button.</summary>
        void SetBusy(string message, bool canCancel);

        void SetLobby(LobbyViewModel lobby);

        void SetSessions(IReadOnlyList<SessionListing> sessions);

        void SetRejoin(string message);

        /// <summary>The overlay shown while the match runs. Pass null to hide it.</summary>
        void SetHud(string status);

        void ShowToast(string message, ToastKind kind);
    }
}
