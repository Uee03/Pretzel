using System;
using Pretzel.Cloud;
using Unity.Services.CloudSave;
using Unity.Services.Core;

namespace Pretzel.Cloud.CloudSave
{
    /// <summary>Maps Cloud Save exceptions to <see cref="CloudError"/>.</summary>
    internal static class UnityCloudSaveErrors
    {
        public static CloudError Map(Exception exception)
        {
            if (exception is CloudSaveRateLimitedException limited)
            {
                return new CloudError(CloudErrorKind.RateLimited, limited.Message, limited.ErrorCode, TimeSpan.FromSeconds(Math.Max(0f, limited.RetryAfter)));
            }

            if (exception is CloudSaveConflictException conflict)
            {
                return new CloudError(CloudErrorKind.Conflict, conflict.Message, conflict.ErrorCode);
            }

            if (exception is CloudSaveValidationException validation)
            {
                return new CloudError(CloudErrorKind.InvalidRequest, validation.Message, validation.ErrorCode);
            }

            if (exception is CloudSaveException saveException)
            {
                return FromReason(saveException);
            }

            if (exception is RequestFailedException failed)
            {
                return CloudErrorMapper.FromServiceErrorCode(failed.ErrorCode, failed.Message);
            }

            if (exception is ServicesInitializationException)
            {
                return new CloudError(CloudErrorKind.NotInitialized, exception.Message);
            }

            return CloudErrorMapper.FromException(exception);
        }

        private static CloudError FromReason(CloudSaveException exception)
        {
            CloudErrorKind kind;
            switch (exception.Reason)
            {
                case CloudSaveExceptionReason.NoInternetConnection:
                    kind = CloudErrorKind.Offline;
                    break;
                case CloudSaveExceptionReason.ProjectIdMissing:
                    kind = CloudErrorKind.NotInitialized;
                    break;
                case CloudSaveExceptionReason.PlayerIdMissing:
                case CloudSaveExceptionReason.AccessTokenMissing:
                    kind = CloudErrorKind.NotSignedIn;
                    break;
                case CloudSaveExceptionReason.InvalidArgument:
                case CloudSaveExceptionReason.KeyLimitExceeded:
                    kind = CloudErrorKind.InvalidRequest;
                    break;
                case CloudSaveExceptionReason.Unauthorized:
                    kind = CloudErrorKind.Unauthorized;
                    break;
                case CloudSaveExceptionReason.NotFound:
                    kind = CloudErrorKind.NotFound;
                    break;
                case CloudSaveExceptionReason.TooManyRequests:
                    kind = CloudErrorKind.RateLimited;
                    break;
                case CloudSaveExceptionReason.ServiceUnavailable:
                    kind = CloudErrorKind.ServiceUnavailable;
                    break;
                case CloudSaveExceptionReason.Conflict:
                    kind = CloudErrorKind.Conflict;
                    break;
                default:
                    return CloudErrorMapper.FromServiceErrorCode(exception.ErrorCode, exception.Message);
            }

            return new CloudError(kind, exception.Message, exception.ErrorCode);
        }
    }
}
