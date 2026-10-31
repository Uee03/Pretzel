namespace Pretzel.Multiplayer
{
    /// <summary>Why a session ended or a connection dropped. Decides whether rejoining is offered.</summary>
    public enum SessionEndReason
    {
        None = 0,

        /// <summary>The player left on purpose.</summary>
        LeftByUser = 1,

        /// <summary>Transport failure, timeout or app suspend. The session may still exist.</summary>
        ConnectionLost = 2,

        /// <summary>The host left or the session was deleted. Rejoining is impossible.</summary>
        HostLeft = 3,

        /// <summary>The player was removed from the session. Rejoining is not offered.</summary>
        Kicked = 4,

        /// <summary>The match ended.</summary>
        MatchEnded = 5,
    }
}
