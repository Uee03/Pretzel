using System;

namespace Pretzel.Multiplayer
{
    /// <summary>What was remembered about the last session so a dropped client can offer to rejoin it. Holds identifiers only, never credentials.</summary>
    [Serializable]
    public sealed class ReconnectRecord
    {
        public const int CurrentVersion = 1;

        [UnityEngine.SerializeField] private int _version = CurrentVersion;
        [UnityEngine.SerializeField] private string _sessionId;
        [UnityEngine.SerializeField] private int _role;
        [UnityEngine.SerializeField] private int _mode;
        [UnityEngine.SerializeField] private int _localPlayerIndex;
        [UnityEngine.SerializeField] private int _localPlayerCount = 1;
        [UnityEngine.SerializeField] private string _displayName;
        [UnityEngine.SerializeField] private bool _crossPlay = true;
        [UnityEngine.SerializeField] private long _savedAtUtcTicks;

        public int Version => _version;

        public string SessionId => _sessionId;

        public SessionRole Role => (SessionRole)_role;

        public SessionMode Mode => (SessionMode)_mode;

        /// <summary>The first LocalPlayerIndex owned by this client.</summary>
        public int LocalPlayerIndex => _localPlayerIndex;

        public int LocalPlayerCount => _localPlayerCount;

        public string DisplayName => _displayName;

        public bool CrossPlay => _crossPlay;

        public DateTime SavedAtUtc => new DateTime(_savedAtUtcTicks, DateTimeKind.Utc);

        public static ReconnectRecord Create(string sessionId, SessionRole role, SessionMode mode, int localPlayerIndex, int localPlayerCount,
            string displayName, bool crossPlay, DateTime utcNow)
        {
            return new ReconnectRecord
            {
                _sessionId = sessionId,
                _role = (int)role,
                _mode = (int)mode,
                _localPlayerIndex = localPlayerIndex,
                _localPlayerCount = localPlayerCount,
                _displayName = displayName,
                _crossPlay = crossPlay,
                _savedAtUtcTicks = utcNow.ToUniversalTime().Ticks,
            };
        }

        public bool IsExpired(DateTime utcNow, TimeSpan maxAge)
        {
            return utcNow.ToUniversalTime() - SavedAtUtc > maxAge;
        }

        /// <summary>Only an online client can rejoin: a host's allocation dies with the host process, and local sessions live in the process.</summary>
        public bool CanRejoin()
        {
            return _version == CurrentVersion && Mode == SessionMode.Online && Role == SessionRole.Client && !string.IsNullOrEmpty(_sessionId);
        }
    }
}
