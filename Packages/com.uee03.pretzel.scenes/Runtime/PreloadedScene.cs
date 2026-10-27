using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using ThreadPriority = UnityEngine.ThreadPriority;

namespace Pretzel.Scenes
{
    /// <summary>
    /// An additive scene loaded in the background and held at 90 %, so it can switch on instantly, for example when the
    /// player reaches a door. While it is held, the loader queues other loads, because Unity cannot finish any other
    /// asynchronous load until this one activates. Activate it or cancel it; do not leave it held.
    /// </summary>
    public sealed class PreloadedScene
    {
        private readonly SceneLoader _loader;

        public string SceneName { get; }

        /// <summary>True once the scene is loaded up to activation.</summary>
        public bool IsReady => Operation.progress >= 0.9f;

        /// <summary>0 to 1 while loading in the background.</summary>
        public float Progress => Mathf.Clamp01(Operation.progress / 0.9f);

        /// <summary>True after <see cref="ActivateAsync"/> or <see cref="CancelAsync"/>; the handle cannot be used again.</summary>
        public bool IsConsumed { get; internal set; }

        internal AsyncOperation Operation { get; }

        internal ThreadPriority OriginalPriority { get; }

        internal PreloadedScene(SceneLoader loader, string sceneName, AsyncOperation operation, ThreadPriority originalPriority)
        {
            _loader = loader;
            SceneName = sceneName;
            Operation = operation;
            OriginalPriority = originalPriority;
        }

        /// <summary>
        /// Activates the scene additively and runs the rest of a normal load: optional transition, setup, settle frames.
        /// <see cref="SceneLoadOptions.Mode"/> is ignored. Skip the transition for a seamless switch.
        /// </summary>
        public Awaitable<Scene> ActivateAsync(SceneLoadOptions options = null, CancellationToken cancellationToken = default)
        {
            return _loader.ActivatePreloadAsync(this, options, cancellationToken);
        }

        /// <summary>Unity cannot abort a load, so this activates the scene and unloads it again.</summary>
        public Awaitable CancelAsync() => _loader.CancelPreloadAsync(this);
    }
}
