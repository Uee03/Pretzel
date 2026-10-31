using System;
using Pretzel.Cloud;
using Unity.Services.Authentication;
using Unity.Services.Core;

namespace Pretzel.Cloud.Authentication
{
    /// <summary>Maps Unity Authentication exceptions to <see cref="CloudError"/>.</summary>
    internal static class UnityAuthenticationErrors
    {
        public static CloudError Map(Exception exception)
        {
            if (exception is AuthenticationException authentication)
            {
                return FromAuthenticationCode(authentication.ErrorCode, authentication.Message);
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

        // AuthenticationErrorCodes are static readonly, so they cannot be switch labels.
        private static CloudError FromAuthenticationCode(int code, string message)
        {
            CloudErrorKind kind;
            if (code == AuthenticationErrorCodes.ClientNoActiveSession)
            {
                kind = CloudErrorKind.NotSignedIn;
            }
            else if (code == AuthenticationErrorCodes.InvalidSessionToken || code == AuthenticationErrorCodes.BannedUser)
            {
                kind = CloudErrorKind.Unauthorized;
            }
            else if (code == AuthenticationErrorCodes.AccountAlreadyLinked)
            {
                kind = CloudErrorKind.Conflict;
            }
            else if (code == AuthenticationErrorCodes.ClientUnlinkExternalIdNotFound)
            {
                kind = CloudErrorKind.NotFound;
            }
            else if (code == AuthenticationErrorCodes.ClientInvalidUserState
                     || code == AuthenticationErrorCodes.InvalidParameters
                     || code == AuthenticationErrorCodes.AccountLinkLimitExceeded
                     || code == AuthenticationErrorCodes.ClientInvalidProfile
                     || code == AuthenticationErrorCodes.InvalidProvider
                     || code == AuthenticationErrorCodes.EnvironmentMismatch)
            {
                kind = CloudErrorKind.InvalidRequest;
            }
            else
            {
                return CloudErrorMapper.FromServiceErrorCode(code, message);
            }

            return new CloudError(kind, message, code);
        }
    }
}
