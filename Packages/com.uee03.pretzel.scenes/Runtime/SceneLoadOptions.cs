using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pretzel.Scenes
{
    public sealed class SceneLoadOptions
    {
        public LoadSceneMode Mode { get; set; } = LoadSceneMode.Single;

        /// <summary>Makes an additively loaded scene the active scene. Single loads are always active.</summary>
        public bool SetActive { get; set; } = true;

        /// <summary>Optional. Shown before loading and hidden after activation and setup.</summary>
        public ISceneTransition Transition { get; set; }

        /// <summary>Real-time seconds the load takes at least, so a fast load does not flash the loading screen.</summary>
        public float MinimumDuration { get; set; }

        /// <summary>Calls Resources.UnloadUnusedAssets after activation, before the transition hides.</summary>
        public bool UnloadUnusedAssets { get; set; }

        /// <summary>Receives 0 to 1 for the whole load, including <see cref="SetupAsync"/>. It never goes backwards.</summary>
        public IProgress<float> Progress { get; set; }

        /// <summary>
        /// Optional work after the scene activated and before the transition hides, such as spawning saved objects,
        /// prewarming pools or warming up shaders. Spread heavy work over frames so the progress bar keeps moving.
        /// </summary>
        public Func<SceneSetupContext, Awaitable> SetupAsync { get; set; }

        /// <summary>Share of the progress bar for the scene load when <see cref="SetupAsync"/> is set. The setup fills the rest.</summary>
        public float SceneProgressWeight { get; set; } = 0.5f;

        /// <summary>Frames to wait after setup before hiding the transition, so physics, animation and layout settle.</summary>
        public int SettleFrames { get; set; } = 1;

        /// <summary>Status shown while the scene loads, when the transition implements <see cref="ISceneStatusDisplay"/>.</summary>
        public string LoadingStatus { get; set; }

        /// <summary>
        /// Application.backgroundLoadingPriority during the load, restored afterwards. High loads fastest behind a loading
        /// screen; Low suits preloading during gameplay. Null leaves it alone.
        /// </summary>
        public ThreadPriority? LoadingPriority { get; set; } = ThreadPriority.High;

        /// <summary>
        /// A scene, for example the main menu, loaded in single mode behind the transition when the load fails.
        /// The loader then throws <see cref="SceneLoadException"/>. Cancellation never falls back.
        /// </summary>
        public string FallbackScene { get; set; }
    }
}
