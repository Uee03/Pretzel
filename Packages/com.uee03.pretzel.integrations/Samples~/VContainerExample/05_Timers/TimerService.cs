using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>
    /// Advances every timer from one <see cref="ITickable.Tick"/>, instead of one coroutine or Update per timer.
    /// Pausing is one flag, because tickables ignore GameObject state.
    /// </summary>
    public sealed class TimerService : ITimerService, ITickable
    {
        private readonly List<Timer> _timers = new();

        public bool Paused { get; set; }

        public Timer Start(float seconds, Action elapsed, bool repeat = false)
        {
            if (seconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), "A timer needs a positive duration.");
            }

            Timer timer = new(seconds, repeat, elapsed);
            _timers.Add(timer);
            return timer;
        }

        public void Tick()
        {
            if (Paused)
            {
                return;
            }

            float deltaTime = Time.deltaTime;

            // Backwards, so finished timers can be removed and timers started by a callback wait for the next frame.
            for (int i = _timers.Count - 1; i >= 0; i--)
            {
                if (_timers[i].Advance(deltaTime))
                {
                    _timers.RemoveAt(i);
                }
            }
        }
    }
}
