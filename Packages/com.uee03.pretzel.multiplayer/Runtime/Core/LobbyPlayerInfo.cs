namespace Pretzel.Multiplayer
{
    /// <summary>A player in the lobby list.</summary>
    public sealed class LobbyPlayerInfo
    {
        public string Id { get; }

        public string DisplayName { get; }

        public string PlatformTag { get; }

        public bool IsHost { get; }

        public bool IsLocal { get; }

        public bool IsReady { get; }

        /// <summary>Players that share this player's device. Local mode lists them as separate rows with <see cref="LocalPlayerIndex"/>.</summary>
        public int LocalPlayerCount { get; }

        /// <summary>The LocalPlayerIndex for local-mode rows, otherwise 0.</summary>
        public int LocalPlayerIndex { get; }

        public LobbyPlayerInfo(string id, string displayName, string platformTag, bool isHost, bool isLocal, bool isReady, int localPlayerCount, int localPlayerIndex = 0)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            PlatformTag = platformTag ?? string.Empty;
            IsHost = isHost;
            IsLocal = isLocal;
            IsReady = isReady;
            LocalPlayerCount = localPlayerCount;
            LocalPlayerIndex = localPlayerIndex;
        }
    }
}
