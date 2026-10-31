namespace Pretzel.Multiplayer
{
    public enum PlayerSpawnTiming
    {
        /// <summary>Player objects spawn as soon as a client connects, even in the lobby.</summary>
        OnConnect = 0,

        /// <summary>Player objects spawn when the host starts the match, and for clients that join or rejoin afterwards.</summary>
        OnMatchStart = 1,
    }
}
