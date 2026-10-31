using System;

namespace Pretzel.Multiplayer
{
    /// <summary>Thrown by backends. The service catches it and reports <see cref="Error"/>.</summary>
    public sealed class MultiplayerException : Exception
    {
        public MultiplayerError Error { get; }

        public MultiplayerException(MultiplayerError error, Exception inner = null)
            : base(error?.Message, inner)
        {
            Error = error ?? new MultiplayerError(MultiplayerErrorCode.Unknown, "Unknown error.");
        }

        public MultiplayerException(MultiplayerErrorCode code, string message, bool isRetryable = false, Exception inner = null)
            : this(new MultiplayerError(code, message, isRetryable), inner)
        {
        }
    }
}
