namespace Pretzel.Multiplayer
{
    /// <summary>Pure lobby rules shared by every backend and the menu presenter.</summary>
    public static class LobbyRules
    {
        /// <summary>The host can start when it is the host and every other player is ready. Local mode has no remote players, so it can always start.</summary>
        public static bool CanStart(LobbySnapshot lobby)
        {
            if (lobby == null || !lobby.LocalIsHost || lobby.MatchStarted || lobby.Players.Count == 0)
            {
                return false;
            }

            if (lobby.Mode == SessionMode.Local)
            {
                return true;
            }

            for (int i = 0; i < lobby.Players.Count; i++)
            {
                LobbyPlayerInfo player = lobby.Players[i];
                if (!player.IsHost && !player.IsReady)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>The local, non-host player's ready flag, or false when not found.</summary>
        public static bool IsLocalReady(LobbySnapshot lobby)
        {
            if (lobby == null)
            {
                return false;
            }

            for (int i = 0; i < lobby.Players.Count; i++)
            {
                if (lobby.Players[i].IsLocal)
                {
                    return lobby.Players[i].IsReady;
                }
            }

            return false;
        }
    }
}
