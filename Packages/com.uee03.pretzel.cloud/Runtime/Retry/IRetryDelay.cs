using System;
using System.Threading;
using UnityEngine;

namespace Pretzel.Cloud
{
    /// <summary>Waits between retry attempts. Replace it in tests to run retries with no real delay.</summary>
    public interface IRetryDelay
    {
        /// <summary>Completes after <paramref name="delay"/>. Cancelling throws <see cref="OperationCanceledException"/>.</summary>
        Awaitable DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
    }
}
