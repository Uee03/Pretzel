using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pretzel.DI
{
    /// <summary>
    /// Calls entry point interfaces on a list of objects. <see cref="LifetimeScope"/> drives one; drive it yourself when
    /// you build a <see cref="Container"/> without a scene component. An exception in one entry point is logged and does not stop the others.
    /// </summary>
    public sealed class EntryPointDispatcher
    {
        private readonly IStartable[] _startables;
        private readonly ITickable[] _tickables;
        private readonly ILateTickable[] _lateTickables;
        private readonly IFixedTickable[] _fixedTickables;
        private bool _started;

        public EntryPointDispatcher(IEnumerable<object> entryPoints)
        {
            List<IStartable> startables = new();
            List<ITickable> tickables = new();
            List<ILateTickable> lateTickables = new();
            List<IFixedTickable> fixedTickables = new();
            foreach (object entryPoint in entryPoints)
            {
                if (entryPoint is IStartable startable)
                {
                    startables.Add(startable);
                }

                if (entryPoint is ITickable tickable)
                {
                    tickables.Add(tickable);
                }

                if (entryPoint is ILateTickable lateTickable)
                {
                    lateTickables.Add(lateTickable);
                }

                if (entryPoint is IFixedTickable fixedTickable)
                {
                    fixedTickables.Add(fixedTickable);
                }
            }

            _startables = startables.ToArray();
            _tickables = tickables.ToArray();
            _lateTickables = lateTickables.ToArray();
            _fixedTickables = fixedTickables.ToArray();
        }

        /// <summary>Calls <see cref="IStartable.Start"/> once. Later calls do nothing.</summary>
        public void Start()
        {
            if (_started)
            {
                return;
            }

            _started = true;
            foreach (IStartable startable in _startables)
            {
                Run(startable.Start);
            }
        }

        public void Tick()
        {
            for (int i = 0; i < _tickables.Length; i++)
            {
                try
                {
                    _tickables[i].Tick();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        public void LateTick()
        {
            for (int i = 0; i < _lateTickables.Length; i++)
            {
                try
                {
                    _lateTickables[i].LateTick();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        public void FixedTick()
        {
            for (int i = 0; i < _fixedTickables.Length; i++)
            {
                try
                {
                    _fixedTickables[i].FixedTick();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        private static void Run(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
