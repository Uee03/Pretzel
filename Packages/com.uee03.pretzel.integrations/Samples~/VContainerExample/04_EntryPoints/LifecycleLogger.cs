using System;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>
    /// Implements every entry point interface and logs the first call of each, in PlayerLoop order. A tickable keeps
    /// running every frame for as long as its scope lives.
    /// </summary>
    public sealed class LifecycleLogger :
        IInitializable, IPostInitializable,
        IStartable, IPostStartable,
        IFixedTickable, IPostFixedTickable,
        ITickable, IPostTickable,
        ILateTickable, IPostLateTickable,
        IDisposable
    {
        private const string Topic = "04 Entry points";

        private readonly ISampleLog _log;
        private bool _fixedTicked;
        private bool _postFixedTicked;
        private bool _ticked;
        private bool _postTicked;
        private bool _lateTicked;
        private bool _postLateTicked;

        public LifecycleLogger(ISampleLog log) => _log = log;

        public void Initialize() => _log.Write(Topic, "IInitializable.Initialize: right after the container is built");

        public void PostInitialize() => _log.Write(Topic, "IPostInitializable.PostInitialize");

        public void Start() => _log.Write(Topic, "IStartable.Start: at Unity's Start timing");

        public void PostStart() => _log.Write(Topic, "IPostStartable.PostStart");

        public void FixedTick() => Once(ref _fixedTicked, "IFixedTickable.FixedTick: every physics step, before MonoBehaviour.FixedUpdate");

        public void PostFixedTick() => Once(ref _postFixedTicked, "IPostFixedTickable.PostFixedTick: after every FixedUpdate");

        public void Tick() => Once(ref _ticked, "ITickable.Tick: every frame, before MonoBehaviour.Update");

        public void PostTick() => Once(ref _postTicked, "IPostTickable.PostTick: every frame, after every Update");

        public void LateTick() => Once(ref _lateTicked, "ILateTickable.LateTick: every frame, before MonoBehaviour.LateUpdate");

        public void PostLateTick() => Once(ref _postLateTicked, "IPostLateTickable.PostLateTick: every frame, after every LateUpdate");

        public void Dispose() => UnityEngine.Debug.Log($"[{Topic}] IDisposable.Dispose: when the scope is destroyed");

        private void Once(ref bool done, string message)
        {
            if (!done)
            {
                done = true;
                _log.Write(Topic, message);
            }
        }
    }
}
