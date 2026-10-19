using Pretzel.Core.Singletons;
using UnityEngine;

namespace Pretzel.Samples.Core
{
    /// <summary>
    /// Scene singleton: lives and dies with its scene. A duplicate in the same scene destroys itself.
    /// </summary>
    public class SceneDirector : Singleton<SceneDirector>
    {
        protected override void Awake()
        {
            base.Awake();
            Debug.Log("SceneDirector ready.", this);
        }
    }
}
