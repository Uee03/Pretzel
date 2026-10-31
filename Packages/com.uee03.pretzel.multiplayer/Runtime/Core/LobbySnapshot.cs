using System.Collections.Generic;

namespace Pretzel.Multiplayer
{
    /// <summary>An immutable view of the current session for the lobby UI. A new snapshot is raised for every change.</summary>
    public sealed class LobbySnapshot
    {
        private static readonly IReadOnlyList<LobbyPlayerInfo> _noPlayers = new LobbyPlayerInfo[0];

        public SessionMode Mode { get; }

        public string SessionId { get; }

        public string SessionName { get; }

        /// <summary>The code other players type to join. Empty in local mode.</summary>
        public string JoinCode { get; }

        public int MaxPlayers { get; }

        public bool IsPrivate { get; }

        public bool LocalIsHost { get; }

        public bool CrossPlay { get; }

        public string PlatformTag { get; }

        public bool MatchStarted { get; }

        public IReadOnlyList<LobbyPlayerInfo> Players { get; }

        public LobbySnapshot(SessionMode mode, string sessionId, string sessionName, string joinCode, int maxPlayers, bool isPrivate, bool localIsHost,
            bool crossPlay, string platformTag, bool matchStarted, IReadOnlyList<LobbyPlayerInfo> players)
        {
            Mode = mode;
            SessionId = sessionId ?? string.Empty;
            SessionName = sessionName ?? string.Empty;
            JoinCode = joinCode ?? string.Empty;
            MaxPlayers = maxPlayers;
            IsPrivate = isPrivate;
            LocalIsHost = localIsHost;
            CrossPlay = crossPlay;
            PlatformTag = platformTag ?? string.Empty;
            MatchStarted = matchStarted;
            Players = players ?? _noPlayers;
        }

        /// <summary>A copy with another match flag, so the service can flip it without rebuilding the player list.</summary>
        public LobbySnapshot WithMatchStarted(bool matchStarted)
        {
            return new LobbySnapshot(Mode, SessionId, SessionName, JoinCode, MaxPlayers, IsPrivate, LocalIsHost, CrossPlay, PlatformTag, matchStarted, Players);
        }
    }
}
