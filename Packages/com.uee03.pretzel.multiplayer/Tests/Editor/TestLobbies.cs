namespace Pretzel.Multiplayer.Tests
{
    public static class TestLobbies
    {
        public static LobbySnapshot Online(bool localIsHost, bool matchStarted = false, bool remoteReady = false)
        {
            LobbyPlayerInfo host = new LobbyPlayerInfo("host", "Host", "windows", true, localIsHost, true, 1);
            LobbyPlayerInfo guest = new LobbyPlayerInfo("guest", "Guest", "android", false, !localIsHost, remoteReady, 1);
            return new LobbySnapshot(SessionMode.Online, "session-1", "Test session", "ABC123", 4, false, localIsHost, true, "windows", matchStarted,
                new[] { host, guest });
        }

        public static LobbySnapshot Local(int players = 2)
        {
            LobbyPlayerInfo[] rows = new LobbyPlayerInfo[players];
            for (int i = 0; i < players; i++)
            {
                rows[i] = new LobbyPlayerInfo("local-" + i, "P" + (i + 1), "windows", i == 0, true, true, players, i);
            }

            return new LobbySnapshot(SessionMode.Local, "local", "Local co-op", string.Empty, players, true, true, false, "windows", false, rows);
        }
    }
}
