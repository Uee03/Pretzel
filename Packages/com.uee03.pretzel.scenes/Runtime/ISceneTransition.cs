using System.Threading;
using UnityEngine;

namespace Pretzel.Scenes
{
    /// <summary>Covers the screen while a scene loads, for example a fade or a loading screen.</summary>
    public interface ISceneTransition
    {
        /// <summary>Completes when the screen is fully covered.</summary>
        Awaitable ShowAsync(CancellationToken cancellationToken);

        /// <summary>Completes when the new scene is fully visible.</summary>
        Awaitable HideAsync(CancellationToken cancellationToken);

        /// <summary>Receives 0 to 1 while the scene loads.</summary>
        void ReportProgress(float progress);
    }
}
