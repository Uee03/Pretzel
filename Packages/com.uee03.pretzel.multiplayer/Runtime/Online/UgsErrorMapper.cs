using System;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Ugs = Unity.Services.Multiplayer;

namespace Pretzel.Multiplayer
{
    /// <summary>Maps Multiplayer Services, Authentication and Core exceptions to <see cref="MultiplayerException"/>. No service type leaves the Online backend.</summary>
    public static class UgsErrorMapper
    {
        public static MultiplayerException Map(Exception exception)
        {
            switch (exception)
            {
                case null:
                    return new MultiplayerException(MultiplayerErrorCode.Unknown, "Unknown error.");
                case MultiplayerException typed:
                    return typed;
                case Ugs.SessionException session:
                    return new MultiplayerException(Classify(session.Error, session.Message), session.Message, IsRetryable(session.Error), session);
                case AuthenticationException auth:
                    return new MultiplayerException(MultiplayerErrorCode.NotSignedIn, "Could not sign in: " + auth.Message, true, auth);
                case RequestFailedException request:
                    return new MultiplayerException(MultiplayerErrorCode.ServicesUnavailable, "A Unity service request failed: " + request.Message, true, request);
                default:
                    return new MultiplayerException(MultiplayerErrorCode.Unknown, exception.Message, false, exception);
            }
        }

        /// <summary>
        /// Best-effort classification. The SDK reports a full session and a missing quick-join match as <c>SessionError.Unknown</c> with the service text,
        /// so those two are recognized from the message. Not verified against a live backend.
        /// </summary>
        public static MultiplayerErrorCode Classify(Ugs.SessionError error, string message)
        {
            switch (error)
            {
                case Ugs.SessionError.SessionNotFound:
                    return MultiplayerErrorCode.SessionNotFound;
                case Ugs.SessionError.SessionDeleted:
                    return MultiplayerErrorCode.SessionEnded;
                case Ugs.SessionError.NotAuthorized:
                case Ugs.SessionError.Forbidden:
                    return MultiplayerErrorCode.NotAuthorized;
                case Ugs.SessionError.RateLimitExceeded:
                    return MultiplayerErrorCode.RateLimited;
                case Ugs.SessionError.InvalidParameter:
                case Ugs.SessionError.InvalidCreateSessionOptions:
                case Ugs.SessionError.InvalidSessionIdentifier:
                case Ugs.SessionError.InvalidPlayerName:
                    return MultiplayerErrorCode.InvalidRequest;
                case Ugs.SessionError.NetworkManagerNotInitialized:
                case Ugs.SessionError.MissingAssembly:
                    return MultiplayerErrorCode.NetworkManagerMissing;
                case Ugs.SessionError.NetworkManagerStartFailed:
                case Ugs.SessionError.NetworkSetupFailed:
                case Ugs.SessionError.TransportComponentMissing:
                case Ugs.SessionError.TransportInvalid:
                case Ugs.SessionError.InvalidNetworkConfig:
                    return MultiplayerErrorCode.ConnectionFailed;
                case Ugs.SessionError.InvalidOperation:
                case Ugs.SessionError.SessionTypeAlreadyExists:
                    return MultiplayerErrorCode.InvalidOperation;
                default:
                    return ClassifyMessage(message);
            }
        }

        private static MultiplayerErrorCode ClassifyMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return MultiplayerErrorCode.Unknown;
            }

            if (message.IndexOf("full", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return MultiplayerErrorCode.SessionFull;
            }

            if (message.IndexOf("no open", StringComparison.OrdinalIgnoreCase) >= 0 || message.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return MultiplayerErrorCode.SessionNotFound;
            }

            if (message.IndexOf("password", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return MultiplayerErrorCode.NotAuthorized;
            }

            return MultiplayerErrorCode.Unknown;
        }

        private static bool IsRetryable(Ugs.SessionError error)
        {
            switch (error)
            {
                case Ugs.SessionError.RateLimitExceeded:
                case Ugs.SessionError.NetworkManagerStartFailed:
                case Ugs.SessionError.NetworkSetupFailed:
                case Ugs.SessionError.OutOfSyncSession:
                case Ugs.SessionError.SessionConflict:
                case Ugs.SessionError.Unknown:
                    return true;
                default:
                    return false;
            }
        }
    }
}
