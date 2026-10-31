using System;

namespace Pretzel.Cloud
{
    /// <summary>What the last Remote Config fetch produced.</summary>
    public sealed class RemoteConfigFetchInfo
    {
        public RemoteConfigOrigin Origin { get; }

        /// <summary>When the fetch finished, in UTC, or null when no fetch has run.</summary>
        public DateTime? FetchedAtUtc { get; }

        public int KeyCount { get; }

        /// <summary>True when the values came from the service in the last fetch, not from the cache or defaults.</summary>
        public bool IsFresh => Origin == RemoteConfigOrigin.Remote;

        public RemoteConfigFetchInfo(RemoteConfigOrigin origin, DateTime? fetchedAtUtc, int keyCount)
        {
            Origin = origin;
            FetchedAtUtc = fetchedAtUtc;
            KeyCount = keyCount;
        }

        public static RemoteConfigFetchInfo None() => new(RemoteConfigOrigin.Default, null, 0);
    }
}
