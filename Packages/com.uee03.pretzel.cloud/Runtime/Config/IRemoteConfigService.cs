using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Pretzel.Cloud
{
    /// <summary>
    /// Server-driven configuration values. Getters never throw and never need a successful fetch: they return the default
    /// you pass when a key is missing or has another type. Use from the main thread.
    /// </summary>
    public interface IRemoteConfigService
    {
        /// <summary>What the last <see cref="FetchAsync"/> produced, or <see cref="RemoteConfigFetchInfo.None"/>.</summary>
        RemoteConfigFetchInfo LastFetch { get; }

        /// <summary>Keys of the current values.</summary>
        IReadOnlyList<string> Keys { get; }

        /// <summary>
        /// Fetches the values for the signed-in player. A failed network fetch does not fail the call when cached values exist:
        /// check <see cref="RemoteConfigFetchInfo.IsFresh"/> on the returned info.
        /// </summary>
        /// <param name="userAttributes">Attributes your rules target on, such as level or country. May be null.</param>
        /// <param name="appAttributes">Attributes about the build, such as version. May be null.</param>
        Awaitable<CloudResult<RemoteConfigFetchInfo>> FetchAsync(IReadOnlyDictionary<string, object> userAttributes = null, IReadOnlyDictionary<string, object> appAttributes = null, CancellationToken cancellationToken = default);

        bool HasKey(string key);

        bool GetBool(string key, bool defaultValue = false);

        int GetInt(string key, int defaultValue = 0);

        long GetLong(string key, long defaultValue = 0L);

        float GetFloat(string key, float defaultValue = 0f);

        string GetString(string key, string defaultValue = "");

        /// <summary>Reads a JSON value as <typeparamref name="T"/> with the service's <c>ISerializer</c>. Returns the default on a missing key or malformed JSON.</summary>
        T GetJson<T>(string key, T defaultValue = default);
    }
}
