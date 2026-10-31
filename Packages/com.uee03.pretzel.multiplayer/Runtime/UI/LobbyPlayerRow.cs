namespace Pretzel.Multiplayer.UI
{
    /// <summary>One formatted row of the lobby player list.</summary>
    public sealed class LobbyPlayerRow
    {
        public string Name { get; }

        public string Platform { get; }

        public string Status { get; }

        public bool IsLocal { get; }

        public bool IsHost { get; }

        public bool IsReady { get; }

        public LobbyPlayerRow(string name, string platform, string status, bool isLocal, bool isHost, bool isReady)
        {
            Name = name;
            Platform = platform;
            Status = status;
            IsLocal = isLocal;
            IsHost = isHost;
            IsReady = isReady;
        }
    }
}
