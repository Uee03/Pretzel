using System;

namespace Pretzel.Multiplayer
{
    /// <summary>Bounded exponential backoff: base, base x factor, base x factor^2 ... capped at the maximum delay, with optional jitter.</summary>
    public sealed class ExponentialBackoffPolicy : IReconnectPolicy
    {
        private readonly int _maxAttempts;
        private readonly TimeSpan _baseDelay;
        private readonly TimeSpan _maxDelay;
        private readonly double _factor;
        private readonly double _jitter;
        private readonly Func<double> _random;

        public int MaxAttempts => _maxAttempts;

        /// <param name="maxAttempts">Zero disables automatic reconnect: the player gets the prompt straight away.</param>
        /// <param name="jitter">Fraction of the delay added or removed at random (0 to 1). Zero is deterministic.</param>
        /// <param name="random">Returns a value in [0, 1). Defaults to a shared System.Random.</param>
        public ExponentialBackoffPolicy(int maxAttempts, TimeSpan baseDelay, TimeSpan maxDelay, double factor = 2d, double jitter = 0d, Func<double> random = null)
        {
            if (maxAttempts < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAttempts));
            }

            if (baseDelay < TimeSpan.Zero || maxDelay < baseDelay)
            {
                throw new ArgumentException("Delays must satisfy 0 <= base <= max.");
            }

            if (factor < 1d || jitter < 0d || jitter > 1d)
            {
                throw new ArgumentOutOfRangeException(nameof(factor), "Factor must be >= 1 and jitter within [0, 1].");
            }

            _maxAttempts = maxAttempts;
            _baseDelay = baseDelay;
            _maxDelay = maxDelay;
            _factor = factor;
            _jitter = jitter;
            _random = random ?? SharedRandom.NextDouble;
        }

        public TimeSpan? GetDelay(int attempt)
        {
            if (attempt < 1 || attempt > _maxAttempts)
            {
                return null;
            }

            double seconds = _baseDelay.TotalSeconds * Math.Pow(_factor, attempt - 1);
            seconds = Math.Min(seconds, _maxDelay.TotalSeconds);
            if (_jitter > 0d)
            {
                seconds *= 1d + _jitter * (_random() * 2d - 1d);
                seconds = Math.Max(0d, Math.Min(seconds, _maxDelay.TotalSeconds));
            }

            return TimeSpan.FromSeconds(seconds);
        }

        private static class SharedRandom
        {
            private static readonly Random _instance = new Random();

            public static double NextDouble()
            {
                return _instance.NextDouble();
            }
        }
    }
}
