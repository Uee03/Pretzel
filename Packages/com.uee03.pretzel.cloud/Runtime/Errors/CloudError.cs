using System;

namespace Pretzel.Cloud
{
    /// <summary>A failed cloud operation. Adapters map SDK exceptions to this, so callers never reference SDK types.</summary>
    public sealed class CloudError
    {
        public CloudErrorKind Kind { get; }

        public string Message { get; }

        /// <summary>The SDK or HTTP error code the error came from, or 0.</summary>
        public int Code { get; }

        /// <summary>How long the service asked the caller to wait, when it said so.</summary>
        public TimeSpan? RetryAfter { get; }

        /// <summary>True when the same request can succeed later without changing it.</summary>
        public bool IsTransient => Kind is CloudErrorKind.Offline or CloudErrorKind.Timeout or CloudErrorKind.RateLimited or CloudErrorKind.ServiceUnavailable;

        /// <summary>True when the player is not signed in or the session is no longer valid.</summary>
        public bool NeedsSignIn => Kind is CloudErrorKind.NotSignedIn or CloudErrorKind.Unauthorized or CloudErrorKind.NotInitialized;

        public CloudError(CloudErrorKind kind, string message, int code = 0, TimeSpan? retryAfter = null)
        {
            Kind = kind;
            Message = message ?? string.Empty;
            Code = code;
            RetryAfter = retryAfter;
        }

        public override string ToString() => $"{Kind}: {Message}";
    }
}
