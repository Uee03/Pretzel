using System;
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

        public Scene Scene { get; }

        /// <summary>Report 0 to 1 for the setup phase; it fills the part of the bar after the scene load.</summary>
        public IProgress<float> Progress { get; }

        public CancellationToken CancellationToken { get; }

        internal SceneSetupContext(Scene scene, IProgress<float> progress, ISceneStatusDisplay status, CancellationToken cancellationToken)
        {
            Scene = scene;
            Progress = progress;
            _status = status;
            CancellationToken = cancellationToken;
        }

        /// <summary>Shows a status line when the transition implements <see cref="ISceneStatusDisplay"/>.</summary>
        public void ReportStatus(string status) => _status?.ReportStatus(status);

        /// <summary>The first component of the type on a root object of the scene or its children, or null.</summary>
        public T FindInScene<T>() where T : class => Scene.FindComponent<T>();
    }
}
