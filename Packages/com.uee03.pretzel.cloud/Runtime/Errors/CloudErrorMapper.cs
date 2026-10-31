using System;
using System.Net.Http;
using System.Net.Sockets;

namespace Pretzel.Cloud
{
    /// <summary>
    /// Pure mapping rules from error codes and plain .NET exceptions to <see cref="CloudError"/>.
    /// Adapters call these after reading the SDK exception, so the table is testable without any SDK.
    /// </summary>
    public static class CloudErrorMapper
    {
        // Values of Unity.Services.Core.CommonErrorCodes (public constants of com.unity.services.core).
        private const int ServiceTransportError = 1;
        private const int ServiceTimeout = 2;
        private const int ServiceUnavailableCode = 3;
        private const int ServiceRequestRejected = 5;
        private const int ServiceTooManyRequests = 50;
        private const int ServiceInvalidToken = 51;
        private const int ServiceTokenExpired = 52;
        private const int ServiceForbidden = 53;
        private const int ServiceNotFound = 54;
        private const int ServiceInvalidRequest = 55;
        private const int ServiceProjectPolicyAccessDenied = 56;
        private const int ServicePlayerPolicyAccessDenied = 57;
        private const int ServiceConflict = 58;

        /// <summary>Maps a Unity Services <c>RequestFailedException.ErrorCode</c> (see <c>CommonErrorCodes</c>).</summary>
        public static CloudError FromServiceErrorCode(int errorCode, string message, TimeSpan? retryAfter = null)
        {
            CloudErrorKind kind = errorCode switch
            {
                ServiceTransportError => CloudErrorKind.Offline,
                ServiceTimeout => CloudErrorKind.Timeout,
                ServiceUnavailableCode => CloudErrorKind.ServiceUnavailable,
                ServiceRequestRejected => CloudErrorKind.InvalidRequest,
                ServiceTooManyRequests => CloudErrorKind.RateLimited,
                ServiceInvalidToken => CloudErrorKind.Unauthorized,
                ServiceTokenExpired => CloudErrorKind.Unauthorized,
                ServiceForbidden => CloudErrorKind.Unauthorized,
                ServiceNotFound => CloudErrorKind.NotFound,
                ServiceInvalidRequest => CloudErrorKind.InvalidRequest,
                ServiceProjectPolicyAccessDenied => CloudErrorKind.Unauthorized,
                ServicePlayerPolicyAccessDenied => CloudErrorKind.Unauthorized,
                ServiceConflict => CloudErrorKind.Conflict,
                _ => CloudErrorKind.Unknown
            };

            return new CloudError(kind, message, errorCode, retryAfter);
        }

        /// <summary>Maps an HTTP status code. A status of 0 means no response arrived.</summary>
        public static CloudError FromHttpStatus(int status, string message, TimeSpan? retryAfter = null)
        {
            CloudErrorKind kind;
            if (status == 0)
            {
                kind = CloudErrorKind.Offline;
            }
            else if (status == 401 || status == 403)
            {
                kind = CloudErrorKind.Unauthorized;
            }
            else if (status == 404)
            {
                kind = CloudErrorKind.NotFound;
            }
            else if (status == 408 || status == 504)
            {
                kind = CloudErrorKind.Timeout;
            }
            else if (status == 409 || status == 412)
            {
                kind = CloudErrorKind.Conflict;
            }
            else if (status == 429)
            {
                kind = CloudErrorKind.RateLimited;
            }
            else if (status >= 500)
            {
                kind = CloudErrorKind.ServiceUnavailable;
            }
            else if (status >= 400)
            {
                kind = CloudErrorKind.InvalidRequest;
            }
            else
            {
                kind = CloudErrorKind.Unknown;
            }

            return new CloudError(kind, message, status, retryAfter);
        }

        /// <summary>
        /// Maps a plain .NET exception. Callers must rethrow <see cref="OperationCanceledException"/> before calling this,
        /// because cancellation is not a cloud failure.
        /// </summary>
        public static CloudError FromException(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            CloudErrorKind kind = exception switch
            {
                TimeoutException => CloudErrorKind.Timeout,
                SocketException => CloudErrorKind.Offline,
                HttpRequestException => CloudErrorKind.Offline,
                ArgumentException => CloudErrorKind.InvalidRequest,
                FormatException => CloudErrorKind.Serialization,
                InvalidCastException => CloudErrorKind.Serialization,
                _ => CloudErrorKind.Unknown
            };

            return new CloudError(kind, exception.Message);
        }
    }
}
