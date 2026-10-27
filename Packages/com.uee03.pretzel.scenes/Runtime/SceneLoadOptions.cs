using System;
using UnityEngine.SceneManagement;

namespace Pretzel.Scenes
{
    public sealed class SceneLoadOptions
    {
        public LoadSceneMode Mode { get; set; } = LoadSceneMode.Single;

        /// <summary>Makes an additively loaded scene the active scene. Single loads are always active.</summary>
        public bool SetActive { get; set; } = true;

        /// <summary>Optional. Shown before loading and hidden after activation.</summary>
        public ISceneTransition Transition { get; set; }

        /// <summary>Real-time seconds the load takes at least, so a fast load does not flash the loading screen.</summary>
        public float MinimumDuration { get; set; }

        /// <summary>Calls Resources.UnloadUnusedAssets after activation, before the transition hides.</summary>
        public bool UnloadUnusedAssets { get; set; }

        /// <summary>Receives 0 to 1 while the scene loads.</summary>
        public IProgress<float> Progress { get; set; }
    }
}
