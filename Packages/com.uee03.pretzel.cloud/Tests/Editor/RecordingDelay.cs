using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Pretzel.Cloud.Tests
{
    /// <summary>Records requested delays and returns at once, so retries run with no real waiting.</summary>
    internal sealed class RecordingDelay : IRetryDelay
    {
        private readonly List<TimeSpan> _delays = new();

        public IReadOnlyList<TimeSpan> Delays => _delays;

        /// <summary>Runs after each recorded delay. A test can cancel a token or change state here.</summary>
        public Action OnDelay { get; set; }

        public async Awaitable DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            _delays.Add(delay);
            await Awaitable.MainThreadAsync();
            cancellationToken.ThrowIfCancellationRequested();
            OnDelay?.Invoke();
        }
    }
}
