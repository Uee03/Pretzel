using System;

namespace Pretzel.Core.Timers
{
    /// <summary>
    /// Counts down from <see cref="Duration"/> while running. The owner calls <see cref="Tick"/> with a delta time,
    /// which keeps the choice of scaled, unscaled or fixed time with the caller.
    /// </summary>
    public sealed class CountdownTimer
    {
        public float Duration { get; private set; }

        public float Remaining { get; private set; }

        public bool IsRunning { get; private set; }

        public bool IsFinished => Remaining <= 0f;

        /// <summary>0 at the start, 1 when finished.</summary>
        public float Progress => Duration <= 0f ? 1f : 1f - Remaining / Duration;

        /// <summary>Raised once when the timer reaches zero.</summary>
        public event Action Completed;

        public CountdownTimer(float duration)
        {
            Duration = Guard.InRange(duration, 0f, float.MaxValue, nameof(duration));
            Remaining = Duration;
        }

        /// <summary>Restarts from the full duration. A zero duration completes on the next tick.</summary>
        public void Start()
        {
            Remaining = Duration;
            IsRunning = true;
        }

        public void Pause() => IsRunning = false;

        /// <summary>Continues a paused countdown. Does nothing once it has finished.</summary>
        public void Resume()
        {
            if (!IsFinished)
            {
                IsRunning = true;
            }
        }

        /// <summary>Stops and rewinds to the full duration without raising <see cref="Completed"/>.</summary>
        public void Reset()
        {
            IsRunning = false;
            Remaining = Duration;
        }

        public void Reset(float duration)
        {
            Duration = Guard.InRange(duration, 0f, float.MaxValue, nameof(duration));
            Reset();
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning)
            {
                return;
            }

            Remaining -= deltaTime;
            if (Remaining > 0f)
            {
                return;
            }

            Remaining = 0f;
            IsRunning = false;
            Completed?.Invoke();
        }
    }
}
