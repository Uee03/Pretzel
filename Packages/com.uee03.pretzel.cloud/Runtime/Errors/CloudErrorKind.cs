namespace Pretzel.Cloud
{
    /// <summary>Why a cloud operation failed, independent of any SDK. Values are stable.</summary>
    public enum CloudErrorKind
    {
        Unknown = 0,

        /// <summary>No connection or the request never reached the service. Retry later.</summary>
        Offline = 1,

        Timeout = 2,

        /// <summary>The service was used before it was initialized, for example before Unity Services started.</summary>
        NotInitialized = 3,

        NotSignedIn = 4,

        /// <summary>The token was rejected or expired, or the player may not do this.</summary>
        Unauthorized = 5,

        RateLimited = 6,

        ServiceUnavailable = 7,

        NotFound = 8,

        /// <summary>A write lock mismatch: the cloud copy changed since it was last read.</summary>
        Conflict = 9,

        /// <summary>The service rejected the request itself: invalid key, value too large, key limit exceeded.</summary>
        InvalidRequest = 10,

        /// <summary>The payload could not be serialized, deserialized or migrated.</summary>
        Serialization = 11,

        /// <summary>Server-side code (a Cloud Code script or module) ran and reported an error. Retrying the same call will not help.</summary>
        ScriptError = 12
    }
}
