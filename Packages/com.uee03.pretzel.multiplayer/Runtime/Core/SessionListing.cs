namespace Pretzel.Multiplayer
{
    /// <summary>One row of the session browser.</summary>
    public sealed class SessionListing
    {
        public string Id { get; }

        public string Name { get; }

        public int PlayerCount { get; }

        public int MaxPlayers { get; }

        public bool HasPassword { get; }

        public bool IsLocked { get; }

        public string PlatformTag { get; }

        public SessionListing(string id, string name, int playerCount, int maxPlayers, bool hasPassword, bool isLocked, string platformTag)
        {
            Id = id;
            Name = name;
            PlayerCount = playerCount;
            MaxPlayers = maxPlayers;
            HasPassword = hasPassword;
            IsLocked = isLocked;
            PlatformTag = platformTag ?? string.Empty;
        }

        public bool IsJoinable => !IsLocked && PlayerCount < MaxPlayers;
    }
}
