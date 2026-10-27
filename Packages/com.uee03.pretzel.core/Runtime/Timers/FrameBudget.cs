using System.Diagnostics;
using System.Threading;
using UnityEngine;

namespace Pretzel.Core.Timers
{
    /// <summary>
    /// Spreads heavy main-thread work, such as spawning many objects, over several frames so the game and a loading
    /// screen keep updating. Call <see cref="YieldIfExceededAsync"/> between work items.
    /// </summary>
    public sealed class FrameBudget
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public float MillisecondsPerFrame { get; set; }

        public bool IsExceeded => _stopwatch.Elapsed.TotalMilliseconds >= MillisecondsPerFrame;

        /// <param name="millisecondsPerFrame">Work allowed per frame. 8 ms leaves room for rendering at 60 fps.</param>
        public FrameBudget(float millisecondsPerFrame = 8f)
        {
            MillisecondsPerFrame = Mathf.Max(0f, millisecondsPerFrame);
        }

        /// <summary>Waits for the next frame when this frame's budget is used up, otherwise returns at once.</summary>
        public async Awaitable YieldIfExceededAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsExceeded)
            {
                return;
            }

            await Awaitable.NextFrameAsync(cancellationToken);
            _stopwatch.Restart();
        }

        /// <summary>Starts a fresh budget, for example at the beginning of a batch.</summary>
        public void Reset() => _stopwatch.Restart();
    }
}
