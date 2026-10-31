using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Pretzel.Core;
using Pretzel.Data;
using UnityEngine;

namespace Pretzel.Cloud.InMemory
{
    /// <summary>
    /// An in-memory <see cref="IRemoteConfigService"/>. <see cref="SetServerValue"/> changes what the "service" holds;
    /// <see cref="FetchAsync"/> copies it into the current values, as a real fetch would.
    /// </summary>
    public sealed class InMemoryRemoteConfigService : IRemoteConfigService
    {
        private readonly ISerializer _serializer;
        private readonly Func<DateTime> _clock;
        private readonly Dictionary<string, object> _server = new(StringComparer.Ordinal);
        private readonly Dictionary<string, object> _current = new(StringComparer.Ordinal);

        public CloudFaults Faults { get; } = new();

        public RemoteConfigFetchInfo LastFetch { get; private set; } = RemoteConfigFetchInfo.None();

        public IReadOnlyList<string> Keys => new List<string>(_current.Keys);

        public InMemoryRemoteConfigService(ISerializer serializer, Func<DateTime> clock = null)
        {
            _serializer = Guard.NotNull(serializer, nameof(serializer));
            _clock = clock ?? (() => DateTime.UtcNow);
        }

        /// <summary>Sets a value on the fake server. It reaches <see cref="IRemoteConfigService"/> getters after the next fetch.</summary>
        public void SetServerValue(string key, object value)
        {
            _server[Guard.NotNullOrEmpty(key, nameof(key))] = value;
        }

        public async Awaitable<CloudResult<RemoteConfigFetchInfo>> FetchAsync(IReadOnlyDictionary<string, object> userAttributes = null, IReadOnlyDictionary<string, object> appAttributes = null, CancellationToken cancellationToken = default)
        {
            await Awaitable.MainThreadAsync();
            cancellationToken.ThrowIfCancellationRequested();
            CloudError fault = Faults.Next();
            if (fault != null && !(fault.Kind is CloudErrorKind.Offline or CloudErrorKind.Timeout))
            {
                return CloudResult<RemoteConfigFetchInfo>.Failure(fault);
            }

            if (fault != null)
            {
                // Like the real service: a failed network fetch keeps the cache and reports where the values come from.
                RemoteConfigOrigin origin = _current.Count > 0 ? RemoteConfigOrigin.Cached : RemoteConfigOrigin.Default;
                LastFetch = new RemoteConfigFetchInfo(origin, _clock(), _current.Count);
                return CloudResult<RemoteConfigFetchInfo>.Success(LastFetch);
            }

            _current.Clear();
            foreach (KeyValuePair<string, object> pair in _server)
            {
                _current[pair.Key] = pair.Value;
            }

            LastFetch = new RemoteConfigFetchInfo(RemoteConfigOrigin.Remote, _clock(), _current.Count);
            return CloudResult<RemoteConfigFetchInfo>.Success(LastFetch);
        }

        public bool HasKey(string key) => key != null && _current.ContainsKey(key);

        public bool GetBool(string key, bool defaultValue = false)
        {
            if (!_current.TryGetValue(key ?? string.Empty, out object value))
            {
                return defaultValue;
            }

            return value is bool flag ? flag : defaultValue;
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            long result = GetLong(key, defaultValue);
            return result >= int.MinValue && result <= int.MaxValue ? (int)result : defaultValue;
        }

        public long GetLong(string key, long defaultValue = 0L)
        {
            if (!_current.TryGetValue(key ?? string.Empty, out object value))
            {
                return defaultValue;
            }

            return value switch
            {
                long number => number,
                int number => number,
                short number => number,
                byte number => number,
                _ => defaultValue
            };
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            if (!_current.TryGetValue(key ?? string.Empty, out object value))
            {
                return defaultValue;
            }

            return value switch
            {
                float number => number,
                double number => (float)number,
                long number => number,
                int number => number,
                _ => defaultValue
            };
        }

        public string GetString(string key, string defaultValue = "")
        {
            if (!_current.TryGetValue(key ?? string.Empty, out object value) || value == null)
            {
                return defaultValue;
            }

            return value as string ?? Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        public T GetJson<T>(string key, T defaultValue = default)
        {
            if (!_current.TryGetValue(key ?? string.Empty, out object value) || value == null)
            {
                return defaultValue;
            }

            try
            {
                string json = value as string ?? _serializer.Serialize(value, false);
                T parsed = _serializer.Deserialize<T>(json);
                return parsed == null ? defaultValue : parsed;
            }
            catch (Exception ex) when (CloudSerialization.IsFailure(ex))
            {
                return defaultValue;
            }
        }
    }
}
