using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.SceneManagement;

namespace Pretzel.Scenes
{
    /// <summary>
    /// Passed to <see cref="SceneLoadOptions.SetupAsync"/> after the scene activated, while the transition still covers it.
    /// </summary>
    public sealed class SceneSetupContext
    {
        private readonly ISceneStatusDisplay _status;

        /// <summary>The active scene of this load.</summary>
        public Scene Scene { get; }

        /// <summary>Every scene this load opened, in load order. One entry unless a <see cref="SceneGroup"/> was loaded.</summary>
        public IReadOnlyList<Scene> Scenes { get; }

        /// <summary>Report 0 to 1 for the setup phase; it fills the part of the bar after the scene load.</summary>
        public IProgress<float> Progress { get; }

        public CancellationToken CancellationToken { get; }

        internal SceneSetupContext(Scene scene, IReadOnlyList<Scene> scenes, IProgress<float> progress, ISceneStatusDisplay status, CancellationToken cancellationToken)
        {
            Scene = scene;
            Scenes = scenes;
            Progress = progress;
            _status = status;
            CancellationToken = cancellationToken;
        }

        /// <summary>Shows a status line when the transition implements <see cref="ISceneStatusDisplay"/>.</summary>
        public void ReportStatus(string status) => _status?.ReportStatus(status);

        /// <summary>The first component of the type in the scenes of this load (active scene first), or null.</summary>
        public T FindInScene<T>() where T : class
        {
            T found = Scene.FindComponent<T>();
            for (int i = 0; found == null && i < Scenes.Count; i++)
            {
                found = Scenes[i].FindComponent<T>();
            }

            return found;
        }
    }
}
