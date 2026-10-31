using System;
using Pretzel.Cloud;
using Unity.Services.CloudCode;
using Unity.Services.Core;

namespace Pretzel.Cloud.CloudCode
{
    /// <summary>Maps Cloud Code exceptions to <see cref="CloudError"/>.</summary>
    internal static class UnityCloudCodeErrors
    {
        public static CloudError Map(Exception exception)
        {
            if (exception is CloudCodeRateLimitedException limited)
            {
                return new CloudError(CloudErrorKind.RateLimited, limited.Message, limited.ErrorCode, TimeSpan.FromSeconds(Math.Max(0, limited.RetryAfter)));
            }

            if (exception is CloudCodeException codeException)
            {
                return FromReason(codeException);
            }

            if (exception is RequestFailedException failed)
            {
                return CloudErrorMapper.FromServiceErrorCode(failed.ErrorCode, failed.Message);
            }

            if (exception is ServicesInitializationException)
            {
                return new CloudError(CloudErrorKind.NotInitialized, exception.Message);
            }

            if (CloudSerialization.IsFailure(exception))
            {
                return new CloudError(CloudErrorKind.Serialization, exception.Message);
            }

            return CloudErrorMapper.FromException(exception);
        }

        private static CloudError FromReason(CloudCodeException exception)
        {
            CloudErrorKind kind;
            switch (exception.Reason)
            {
                case CloudCodeExceptionReason.NoInternetConnection:
                    kind = CloudErrorKind.Offline;
                    break;
                case CloudCodeExceptionReason.ProjectIdMissing:
                    kind = CloudErrorKind.NotInitialized;
                    break;
                case CloudCodeExceptionReason.PlayerIdMissing:
                case CloudCodeExceptionReason.AccessTokenMissing:
                    kind = CloudErrorKind.NotSignedIn;
                    break;
                case CloudCodeExceptionReason.InvalidArgument:
                    kind = CloudErrorKind.InvalidRequest;
                    break;
                case CloudCodeExceptionReason.Unauthorized:
                    kind = CloudErrorKind.Unauthorized;
                    break;
                case CloudCodeExceptionReason.NotFound:
                    kind = CloudErrorKind.NotFound;
                    break;
                case CloudCodeExceptionReason.TooManyRequests:
                    kind = CloudErrorKind.RateLimited;
                    break;
                case CloudCodeExceptionReason.ServiceUnavailable:
                    kind = CloudErrorKind.ServiceUnavailable;
                    break;
                case CloudCodeExceptionReason.ScriptError:
                    kind = CloudErrorKind.ScriptError;
                    break;
                default:
                    return CloudErrorMapper.FromServiceErrorCode(exception.ErrorCode, exception.Message);
            }

            return new CloudError(kind, exception.Message, exception.ErrorCode);
        }
    }
}
