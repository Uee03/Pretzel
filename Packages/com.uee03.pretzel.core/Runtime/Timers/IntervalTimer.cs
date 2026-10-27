using System;

namespace Pretzel.Core.Timers
{
    /// <summary>
    /// Raises <see cref="Elapsed"/> every <see cref="Interval"/> seconds while running, catching up when one tick spans several intervals.
    /// The owner calls <see cref="Tick"/> with a delta time.
    /// </summary>
    public sealed class IntervalTimer
    {
        private float _accumulated;

        public float Interval { get; }

        public bool IsRunning { get; private set; }

        public event Action Elapsed;

        public IntervalTimer(float interval)
        {
            if (!(interval > 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(interval), interval, "Interval must be greater than 0.");
            }

            Interval = interval;
        }

        public void Start() => IsRunning = true;

        public void Stop() => IsRunning = false;

        public void Reset()
        {
            IsRunning = false;
            _accumulated = 0f;
        }

        /// <returns>How many intervals elapsed during this tick.</returns>
        public int Tick(float deltaTime)
        {
            if (!IsRunning)
            {
                return 0;
            }

            _accumulated += deltaTime;
            int count = 0;
            while (_accumulated >= Interval && IsRunning)
            {
                _accumulated -= Interval;
                count++;
                Elapsed?.Invoke();
            }

            return count;
        }
    }
}
