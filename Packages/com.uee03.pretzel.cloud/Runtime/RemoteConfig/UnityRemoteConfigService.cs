using System;
using System.Collections.Generic;
using System.Threading;
using Pretzel.Core;
using Pretzel.Data;
using Unity.Services.RemoteConfig;
using UnityEngine;

namespace Pretzel.Cloud.RemoteConfig
{
    /// <summary>
    /// <see cref="IRemoteConfigService"/> on Unity Remote Config, using the "settings" config type of <c>RemoteConfigService.Instance</c>.
    /// Unity Services must be initialized and the player signed in before <see cref="FetchAsync"/>. The SDK does not throw for a failed
    /// network fetch: it keeps the cached values, which this reports through <see cref="RemoteConfigFetchInfo.Origin"/>.
    /// The fetch takes no cancellation token, so cancelling stops the wait only. Use from the main thread.
    /// </summary>
    public sealed class UnityRemoteConfigService : IRemoteConfigService
    {
        private readonly ISerializer _serializer;
        private readonly Func<DateTime> _clock;

        public RemoteConfigFetchInfo LastFetch { get; private set; } = RemoteConfigFetchInfo.None();

        public IReadOnlyList<string> Keys => new List<string>(Config.GetKeys());

        private static RuntimeConfig Config => RemoteConfigService.Instance.appConfig;

        /// <param name="serializer">Reads <see cref="GetJson{T}"/> values.</param>
        /// <param name="clock">Returns the UTC time recorded as the fetch time. Defaults to <see cref="DateTime.UtcNow"/>.</param>
        public UnityRemoteConfigService(ISerializer serializer, Func<DateTime> clock = null)
        {
            _serializer = Guard.NotNull(serializer, nameof(serializer));
            _clock = clock ?? (() => DateTime.UtcNow);
        }

        public async Awaitable<CloudResult<RemoteConfigFetchInfo>> FetchAsync(IReadOnlyDictionary<string, object> userAttributes = null, IReadOnlyDictionary<string, object> appAttributes = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                Dictionary<string, object> user = Copy(userAttributes);
                Dictionary<string, object> app = Copy(appAttributes);
                RuntimeConfig config = await RemoteConfigService.Instance.FetchConfigsAsync(user, app).WithCancellationAsync(cancellationToken);
                RemoteConfigFetchInfo info = new(Map(config.origin), _clock(), config.GetKeys().Length);
                LastFetch = info;
                return CloudResult<RemoteConfigFetchInfo>.Success(info);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return CloudResult<RemoteConfigFetchInfo>.Failure(CloudErrorMapper.FromException(ex));
            }
        }

        public bool HasKey(string key) => !string.IsNullOrEmpty(key) && Config.HasKey(key);

        public bool GetBool(string key, bool defaultValue = false) => Config.GetBool(key, defaultValue);

        public int GetInt(string key, int defaultValue = 0) => Config.GetInt(key, defaultValue);

        public long GetLong(string key, long defaultValue = 0L) => Config.GetLong(key, defaultValue);

        public float GetFloat(string key, float defaultValue = 0f) => Config.GetFloat(key, defaultValue);

        public string GetString(string key, string defaultValue = "") => Config.GetString(key, defaultValue);

        public T GetJson<T>(string key, T defaultValue = default)
        {
            if (!HasKey(key))
            {
                return defaultValue;
            }

            try
            {
                T parsed = _serializer.Deserialize<T>(Config.GetJson(key, string.Empty));
                return parsed == null ? defaultValue : parsed;
            }
            catch (Exception ex) when (CloudSerialization.IsFailure(ex))
            {
                return defaultValue;
            }
        }

        private static Dictionary<string, object> Copy(IReadOnlyDictionary<string, object> attributes)
        {
            Dictionary<string, object> copy = new(StringComparer.Ordinal);
            if (attributes != null)
            {
                foreach (KeyValuePair<string, object> pair in attributes)
                {
                    copy[pair.Key] = pair.Value;
                }
            }

            return copy;
        }

        private static RemoteConfigOrigin Map(ConfigOrigin origin)
        {
            switch (origin)
            {
                case ConfigOrigin.Remote:
                    return RemoteConfigOrigin.Remote;
                case ConfigOrigin.Cached:
                    return RemoteConfigOrigin.Cached;
                default:
                    return RemoteConfigOrigin.Default;
            }
        }
    }
}
