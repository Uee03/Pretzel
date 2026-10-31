using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pretzel.Multiplayer
{
    /// <summary>
    /// One place for the network configuration the package applies to the NetworkManager and UnityTransport. The values are starting points chosen for a small co-op game,
    /// not measured recommendations: profile your own game before changing the tick rate or timeouts.
    /// </summary>
    [Serializable]
    public sealed class NetworkSettings
    {
        public const int MinTickRate = 10;
        public const int MaxTickRate = 120;

        [Header("Netcode")]
        [Tooltip("NetworkConfig.TickRate. Higher values lower latency of tick-driven state and cost more CPU and bandwidth.")]
        [SerializeField, Range(MinTickRate, MaxTickRate)] private int _tickRate = 30;

        [Tooltip("NetworkConfig.ProtocolVersion. Builds with different values cannot connect to each other.")]
        [SerializeField] private int _protocolVersion;

        [Header("Players")]
        [Tooltip("Maximum clients per session, including the host. Enforced by connection approval.")]
        [SerializeField, Range(1, 64)] private int _maxPlayers = 4;

        [Tooltip("Players that can share one device (couch co-op). Each owned by that device's client.")]
        [SerializeField, Range(1, 8)] private int _maxLocalPlayersPerClient = 2;

        [Tooltip("Connection approval payloads larger than this are rejected. Keep the payload small: it is sent on every connect.")]
        [SerializeField, Range(64, 4096)] private int _maxConnectionDataBytes = 512;

        [Tooltip("NetworkConfig.ClientConnectionBufferTimeout: seconds the server waits for approval to complete.")]
        [SerializeField, Range(1, 60)] private int _approvalTimeoutSeconds = 10;

        [SerializeField] private PlayerSpawnTiming _spawnTiming = PlayerSpawnTiming.OnMatchStart;

        [Header("Reconnect")]
        [Tooltip("Seconds the host keeps a dropped client's slot and objects before despawning them. 0 despawns immediately.")]
        [SerializeField, Range(0f, 600f)] private float _reconnectGraceSeconds = 60f;

        [Tooltip("Automatic reconnect attempts before the Rejoin / Return to lobby prompt. 0 shows the prompt immediately.")]
        [SerializeField, Range(0, 20)] private int _reconnectMaxAttempts = 5;

        [SerializeField, Range(0.1f, 30f)] private float _reconnectBaseDelaySeconds = 1f;

        [SerializeField, Range(0.1f, 120f)] private float _reconnectMaxDelaySeconds = 15f;

        [Tooltip("A remembered session older than this is no longer offered for rejoin.")]
        [SerializeField, Range(1, 1440)] private int _reconnectRecordMaxAgeMinutes = 30;

        [Header("Transport (0 keeps the UnityTransport default)")]
        [SerializeField, Min(0)] private int _connectTimeoutMs;

        [SerializeField, Min(0)] private int _maxConnectAttempts;

        [SerializeField, Min(0)] private int _disconnectTimeoutMs;

        [Header("Local and online")]
        [SerializeField, Range(1024, 65535)] private int _loopbackPort = 7777;

        [Tooltip("Optional Unity Authentication profile, for running several players on one machine. Letters, digits, '-' and '_' only, up to 30 characters.")]
        [SerializeField] private string _authenticationProfile = string.Empty;

        [Tooltip("When false the session is locked while the match runs, so nobody can join late. Existing players can still rejoin.")]
        [SerializeField] private bool _allowLateJoin = true;

        public int TickRate => _tickRate;

        public ushort ProtocolVersion => (ushort)Mathf.Clamp(_protocolVersion, 0, ushort.MaxValue);

        public int MaxPlayers => _maxPlayers;

        public int MaxLocalPlayersPerClient => _maxLocalPlayersPerClient;

        /// <summary>Upper bound for player objects in the scene: every client with its maximum local players.</summary>
        public int MaxPlayerObjects => _maxPlayers * _maxLocalPlayersPerClient;

        public int MaxConnectionDataBytes => _maxConnectionDataBytes;

        public int ApprovalTimeoutSeconds => _approvalTimeoutSeconds;

        public PlayerSpawnTiming SpawnTiming => _spawnTiming;

        public TimeSpan ReconnectGrace => TimeSpan.FromSeconds(_reconnectGraceSeconds);

        public int ReconnectMaxAttempts => _reconnectMaxAttempts;

        public TimeSpan ReconnectBaseDelay => TimeSpan.FromSeconds(_reconnectBaseDelaySeconds);

        public TimeSpan ReconnectMaxDelay => TimeSpan.FromSeconds(_reconnectMaxDelaySeconds);

        public TimeSpan ReconnectRecordMaxAge => TimeSpan.FromMinutes(_reconnectRecordMaxAgeMinutes);

        public int ConnectTimeoutMs => _connectTimeoutMs;

        public int MaxConnectAttempts => _maxConnectAttempts;

        public int DisconnectTimeoutMs => _disconnectTimeoutMs;

        public int LoopbackPort => _loopbackPort;

        public string AuthenticationProfile => _authenticationProfile ?? string.Empty;

        public bool AllowLateJoin => _allowLateJoin;

        public IReconnectPolicy CreateReconnectPolicy()
        {
            return new ExponentialBackoffPolicy(_reconnectMaxAttempts, ReconnectBaseDelay, ReconnectMaxDelay, 2d, 0.2d);
        }

        /// <summary>Returns a description of every invalid value. An empty list means the settings can be applied.</summary>
        public IReadOnlyList<string> Validate()
        {
            List<string> problems = new List<string>();
            if (_tickRate < MinTickRate || _tickRate > MaxTickRate)
            {
                problems.Add($"Tick rate must be between {MinTickRate} and {MaxTickRate}.");
            }

            if (_maxPlayers < 1)
            {
                problems.Add("Max players must be at least 1.");
            }

            if (_maxLocalPlayersPerClient < 1)
            {
                problems.Add("Max local players per client must be at least 1.");
            }

            if (_maxConnectionDataBytes < 64)
            {
                problems.Add("Max connection data must be at least 64 bytes.");
            }

            if (_approvalTimeoutSeconds < 1)
            {
                problems.Add("Approval timeout must be at least 1 second.");
            }

            if (_reconnectGraceSeconds < 0f)
            {
                problems.Add("Reconnect grace cannot be negative.");
            }

            if (_reconnectBaseDelaySeconds <= 0f || _reconnectMaxDelaySeconds < _reconnectBaseDelaySeconds)
            {
                problems.Add("Reconnect delays must satisfy 0 < base <= max.");
            }

            if (_reconnectMaxAttempts < 0)
            {
                problems.Add("Reconnect attempts cannot be negative.");
            }

            if (_loopbackPort < 1024 || _loopbackPort > 65535)
            {
                problems.Add("Loopback port must be between 1024 and 65535.");
            }

            if (!IsValidProfile(_authenticationProfile))
            {
                problems.Add("Authentication profile may only contain letters, digits, '-' and '_' and be at most 30 characters.");
            }

            return problems;
        }

        public static bool IsValidProfile(string profile)
        {
            if (string.IsNullOrEmpty(profile))
            {
                return true;
            }

            if (profile.Length > 30)
            {
                return false;
            }

            foreach (char c in profile)
            {
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' || c == '_';
                if (!ok)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
