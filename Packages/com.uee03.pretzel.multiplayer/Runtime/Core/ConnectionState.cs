namespace Pretzel.Multiplayer
{
    /// <summary>Where the local player is in the session lifecycle. The menu reacts to this and nothing else.</summary>
    public enum ConnectionState
    {
        /// <summary>No session. The menu shows the mode picker, or the rejoin dialog when a dropped session is remembered.</summary>
        Disconnected = 0,

        /// <summary>Creating or joining a session.</summary>
        Connecting = 1,

        /// <summary>In a session and waiting for the match to start (player list, ready flags).</summary>
        InLobby = 2,

        /// <summary>The match is running.</summary>
        Playing = 3,

        /// <summary>The connection dropped and the reconnect policy is retrying.</summary>
        Reconnecting = 4,

        /// <summary>Automatic reconnect gave up. The player chooses Rejoin match or Return to lobby.</summary>
        ReconnectPrompt = 5,

        /// <summary>Leaving the session.</summary>
        Disconnecting = 6,
    }
}
