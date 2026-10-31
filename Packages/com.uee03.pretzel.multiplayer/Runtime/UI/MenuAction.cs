namespace Pretzel.Multiplayer.UI
{
    /// <summary>What the player did in the menu. The view translates clicks into these, the presenter decides what happens.</summary>
    public enum MenuAction
    {
        PickLocal = 0,
        PickHost = 1,
        PickJoin = 2,
        PickQuickJoin = 3,
        Back = 4,
        SubmitHost = 5,
        SubmitJoin = 6,
        RefreshSessions = 7,
        ToggleReady = 8,
        StartMatch = 9,
        Leave = 10,
        Cancel = 11,
        Rejoin = 12,
        ReturnToLobby = 13,
        CrossPlayChanged = 14,
    }
}
