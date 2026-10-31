using System;
using UnityEngine;

namespace Pretzel.Multiplayer
{
    /// <summary>The small payload a client sends with its connection request. Contains no secrets.</summary>
    [Serializable]
    public sealed class ConnectionPayload
    {
        public const int CurrentVersion = 1;

        [SerializeField] private int _version = CurrentVersion;
        [SerializeField] private string _playerKey;
        [SerializeField] private string _displayName;
        [SerializeField] private string _platformTag;
        [SerializeField] private int _localPlayerCount = 1;

        public int Version => _version;

        /// <summary>Identifies the device for slot reservation: the Authentication player id online.</summary>
        public string PlayerKey => _playerKey;

        public string DisplayName => _displayName;

        public string PlatformTag => _platformTag;

        public int LocalPlayerCount => _localPlayerCount;

        public ConnectionPayload()
        {
        }

        public ConnectionPayload(string playerKey, string displayName, string platformTag, int localPlayerCount)
        {
            _playerKey = playerKey;
            _displayName = displayName;
            _platformTag = platformTag;
            _localPlayerCount = localPlayerCount;
        }
    }
}
