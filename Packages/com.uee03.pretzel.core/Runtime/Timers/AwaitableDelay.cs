using System.Threading;
using UnityEngine;

namespace Pretzel.Core.Timers
{
    /// <summary>Delays on the main thread. Cancelling throws <see cref="System.OperationCanceledException"/>.</summary>
    public static class AwaitableDelay
    {
        /// <param name="unscaled">When true, ignores <see cref="Time.timeScale"/>, so it keeps running while the game is paused.</param>
        public static async Awaitable SecondsAsync(float seconds, bool unscaled = false, CancellationToken cancellationToken = default)
        {
            if (!unscaled)
            {
                await Awaitable.WaitForSecondsAsync(seconds, cancellationToken);
                return;
            }

            float end = Time.unscaledTime + seconds;
            while (Time.unscaledTime < end)
            {
                await Awaitable.NextFrameAsync(cancellationToken);
            }
        }

        public static async Awaitable FramesAsync(int frames, CancellationToken cancellationToken = default)
        {
            for (int i = 0; i < frames; i++)
            {
                await Awaitable.NextFrameAsync(cancellationToken);
            }
        }
    }
}
