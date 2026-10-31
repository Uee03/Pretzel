using System;
using Pretzel.Core;

namespace Pretzel.Cloud
{
    /// <summary>
    /// Exponential backoff with jitter. Pure rules: no clock, no randomness of its own.
    /// Only transient errors are retried; a server <see cref="CloudError.RetryAfter"/> longer than <see cref="MaxDelay"/> is not waited for.
    /// </summary>
    public sealed class RetryPolicy
    {
        /// <summary>Total attempts including the first. 1 means no retry.</summary>
        public int MaxAttempts { get; }

        public TimeSpan InitialDelay { get; }

        public double Multiplier { get; }

        public TimeSpan MaxDelay { get; }

        /// <summary>Fraction of the delay that jitter may add or remove, from 0 to 1.</summary>
        public double JitterRatio { get; }

        public RetryPolicy(int maxAttempts = 3, TimeSpan? initialDelay = null, double multiplier = 2d, TimeSpan? maxDelay = null, double jitterRatio = 0.2d)
        {
            MaxAttempts = Guard.InRange(maxAttempts, 1, 100, nameof(maxAttempts));
            InitialDelay = initialDelay ?? TimeSpan.FromSeconds(1);
            MaxDelay = maxDelay ?? TimeSpan.FromSeconds(30);
            if (InitialDelay < TimeSpan.Zero || MaxDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(initialDelay), "Delays cannot be negative.");
            }

            Multiplier = Guard.InRange((float)multiplier, 1f, 100f, nameof(multiplier));
            JitterRatio = Guard.InRange((float)jitterRatio, 0f, 1f, nameof(jitterRatio));
        }

        /// <summary>One attempt, no waiting. For foreground calls that must not stall on a bad connection.</summary>
        public static RetryPolicy NoRetry() => new(1);

        /// <summary>True when another attempt should follow <paramref name="attemptsMade"/> failed attempts.</summary>
        public bool ShouldRetry(CloudError error, int attemptsMade)
        {
            Guard.NotNull(error, nameof(error));
            if (attemptsMade >= MaxAttempts || !error.IsTransient)
            {
                return false;
            }

            return !error.RetryAfter.HasValue || error.RetryAfter.Value <= MaxDelay;
        }

        /// <summary>The wait before the next attempt, after <paramref name="attemptsMade"/> failed attempts (1 or more).</summary>
        /// <param name="jitterSample">A random number from 0 to 1. 0.5 adds no jitter.</param>
        public TimeSpan GetDelay(int attemptsMade, CloudError error, double jitterSample = 0.5d)
        {
            int exponent = Math.Max(0, attemptsMade - 1);
            double seconds = InitialDelay.TotalSeconds * Math.Pow(Multiplier, exponent);
            seconds = Math.Min(seconds, MaxDelay.TotalSeconds);
            double sample = Math.Min(1d, Math.Max(0d, jitterSample));
            seconds *= 1d + (JitterRatio * ((2d * sample) - 1d));
            if (error != null && error.RetryAfter.HasValue)
            {
                seconds = Math.Max(seconds, error.RetryAfter.Value.TotalSeconds);
            }

            return TimeSpan.FromSeconds(Math.Max(0d, seconds));
        }
    }
}
