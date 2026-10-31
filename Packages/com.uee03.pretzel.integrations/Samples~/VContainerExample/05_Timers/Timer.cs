using System;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>A countdown owned by <see cref="TimerService"/>. Cancel it to stop it early.</summary>
    public sealed class Timer
    {
        private readonly Action _elapsed;
        private float _remaining;

        internal Timer(float seconds, bool repeat, Action elapsed)
        {
            Duration = seconds;
            Repeat = repeat;
            _elapsed = elapsed;
            _remaining = seconds;
        }

        public float Duration { get; }

        public bool Repeat { get; }

        public bool IsDone { get; private set; }

        public void Cancel() => IsDone = true;

        /// <summary>Returns true when the timer has finished and can be removed.</summary>
        internal bool Advance(float deltaTime)
        {
            if (IsDone)
            {
                return true;
            }

            _remaining -= deltaTime;
            if (_remaining > 0f)
            {
                return false;
            }

            if (Repeat)
            {
                _remaining += Duration;
            }
            else
            {
                IsDone = true;
            }

            _elapsed();
            return IsDone;
        }
    }
}
