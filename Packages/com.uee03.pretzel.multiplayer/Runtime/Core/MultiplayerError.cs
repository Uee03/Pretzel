using System;

namespace Pretzel.Multiplayer
{
    /// <summary>A failure or notice reported by the service. The message is safe to show to players.</summary>
    public sealed class MultiplayerError
    {
        public MultiplayerErrorCode Code { get; }

        public string Message { get; }

        /// <summary>True when retrying the same operation can succeed, for example a transport failure.</summary>
        public bool IsRetryable { get; }

        public MultiplayerError(MultiplayerErrorCode code, string message, bool isRetryable = false)
        {
            Code = code;
            Message = message ?? string.Empty;
            IsRetryable = isRetryable;
        }

        public static MultiplayerError FromException(Exception exception)
        {
            if (exception is MultiplayerException typed)
            {
                return typed.Error;
            }

            return new MultiplayerError(MultiplayerErrorCode.Unknown, exception?.Message ?? "Unknown error.");
        }

        public override string ToString()
        {
            return $"{Code}: {Message}";
        }
    }
}
