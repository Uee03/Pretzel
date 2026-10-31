using System;

namespace Pretzel.Multiplayer
{
    /// <summary>Raised before each automatic reconnect attempt so the UI can show progress.</summary>
    public sealed class ReconnectProgress
    {
        /// <summary>The 1-based attempt that is about to run after <see cref="Delay"/>.</summary>
        public int Attempt { get; }

        public TimeSpan Delay { get; }

        public ReconnectProgress(int attempt, TimeSpan delay)
        {
            Attempt = attempt;
            Delay = delay;
        }
    }
}
