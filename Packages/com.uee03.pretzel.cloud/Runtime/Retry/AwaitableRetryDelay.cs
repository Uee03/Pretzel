using System;
using System.Threading;
using Pretzel.Core.Timers;
using UnityEngine;

namespace Pretzel.Cloud
{
    /// <summary>Waits on the main thread in unscaled time, so a paused game still retries.</summary>
    public sealed class AwaitableRetryDelay : IRetryDelay
    {
        public Awaitable DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            return AwaitableDelay.SecondsAsync((float)delay.TotalSeconds, true, cancellationToken);
        }
    }
}
