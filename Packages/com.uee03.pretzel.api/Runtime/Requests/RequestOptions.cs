using System;
using System.Collections.Generic;

namespace Pretzel.Api
{
    /// <summary>
    /// Per-request settings. Values left null fall back to <see cref="ApiClient.Defaults"/>. Headers are merged, and these win.
    /// </summary>
    public sealed class RequestOptions
    {
        private readonly Dictionary<string, string> _headers = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Header names are case-insensitive. Use <see cref="SetHeader"/> to add one.</summary>
        public IReadOnlyDictionary<string, string> Headers => _headers;

        /// <summary>Per attempt. 0 means no timeout.</summary>
        public float? TimeoutSeconds { get; set; }

        /// <summary>Extra attempts after a connection error, timeout, 408, 429 or a 5xx status. Only set this for idempotent calls.</summary>
        public int? Retries { get; set; }

        /// <summary>Delay before the first retry. It doubles on each further retry.</summary>
        public float? RetryDelaySeconds { get; set; }

        /// <summary>Content type of request bodies. Defaults to application/json.</summary>
        public string ContentType { get; set; }

        /// <summary>Receives 0 to 1 while the request runs.</summary>
        public IProgress<float> Progress { get; set; }

        public RequestOptions SetHeader(string name, string value)
        {
            _headers[name] = value;
            return this;
        }

        public bool RemoveHeader(string name) => _headers.Remove(name);

        public RequestOptions SetBearerToken(string token) => SetHeader("Authorization", "Bearer " + token);
    }
}
