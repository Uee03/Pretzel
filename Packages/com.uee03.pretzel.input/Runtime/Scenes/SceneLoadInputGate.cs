using System;
using Pretzel.Scenes;

namespace Pretzel.Inputs.Scenes
{
    /// <summary>
    /// Blocks input while a scene loads and releases it afterwards. Pretzel Scenes raises LoadFinished after every load,
    /// successful or not, so input can never stay blocked by a failed load.
    /// </summary>
    public sealed class SceneLoadInputGate : IDisposable
    {
        public const string Reason = "scene-load";

        private readonly SceneLoader _loader;
        private readonly InputRouter _router;

        public SceneLoadInputGate(SceneLoader loader, InputRouter router)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _loader.LoadStarted += OnStarted;
            _loader.LoadFinished += OnFinished;
        }

        public void Dispose()
        {
            _loader.LoadStarted -= OnStarted;
            _loader.LoadFinished -= OnFinished;
            _router.Blockers.Unblock(Reason);
        }

        private void OnStarted(SceneLoadReport report)
        {
            _router.Blockers.Block(Reason);
        }

        private void OnFinished(SceneLoadReport report)
        {
            _router.Blockers.Unblock(Reason);
        }
    }
}
