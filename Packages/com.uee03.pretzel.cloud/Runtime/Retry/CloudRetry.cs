using System;
using System.Threading;
using Pretzel.Core;
using UnityEngine;

namespace Pretzel.Cloud
{
    /// <summary>
    /// Runs a cloud operation again after transient failures, following a <see cref="RetryPolicy"/>.
    /// The operation reports failure as a result; an exception it throws (including cancellation) is not retried.
    /// </summary>
    public sealed class CloudRetry
    {
        private readonly IRetryDelay _delay;
        private readonly Func<double> _random;

        public RetryPolicy Policy { get; }

        /// <param name="policy">Defaults to three attempts with one and two seconds between them.</param>
        /// <param name="delay">Defaults to <see cref="AwaitableRetryDelay"/>.</param>
        /// <param name="random">Returns a number from 0 to 1 for jitter. Defaults to <see cref="System.Random"/>.</param>
        public CloudRetry(RetryPolicy policy = null, IRetryDelay delay = null, Func<double> random = null)
        {
            Policy = policy ?? new RetryPolicy();
            _delay = delay ?? new AwaitableRetryDelay();
            _random = random ?? new System.Random().NextDouble;
        }

        /// <summary>Cancelling throws <see cref="OperationCanceledException"/>, also while waiting between attempts.</summary>
        public async Awaitable<CloudResult<T>> RunAsync<T>(Func<CancellationToken, Awaitable<CloudResult<T>>> operation, CancellationToken cancellationToken = default)
        {
            Guard.NotNull(operation, nameof(operation));
            int attempts = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CloudResult<T> result = await operation(cancellationToken);
                attempts++;
                if (result.IsSuccess || !Policy.ShouldRetry(result.Error, attempts))
                {
                    return result;
                }

                await _delay.DelayAsync(Policy.GetDelay(attempts, result.Error, _random()), cancellationToken);
            }
        }

        /// <inheritdoc cref="RunAsync{T}"/>
        public async Awaitable<CloudResult> RunAsync(Func<CancellationToken, Awaitable<CloudResult>> operation, CancellationToken cancellationToken = default)
        {
            Guard.NotNull(operation, nameof(operation));
            int attempts = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CloudResult result = await operation(cancellationToken);
                attempts++;
                if (result.IsSuccess || !Policy.ShouldRetry(result.Error, attempts))
                {
                    return result;
                }

                await _delay.DelayAsync(Policy.GetDelay(attempts, result.Error, _random()), cancellationToken);
            }
        }
    }
}
