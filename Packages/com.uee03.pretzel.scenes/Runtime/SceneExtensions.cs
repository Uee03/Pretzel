using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace Pretzel.Scenes
{
    public static class SceneExtensions
    {
        /// <summary>
        /// The first component of the type on the scene's root objects or their children, including inactive ones, or null.
        /// Searches only this scene, unlike FindAnyObjectByType.
        /// </summary>
        public static T FindComponent<T>(this Scene scene) where T : class
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            List<GameObject> roots = ListPool<GameObject>.Get();
            try
            {
                scene.GetRootGameObjects(roots);
                foreach (GameObject root in roots)
                {
                    T found = root.GetComponentInChildren<T>(true);
                    if (found != null)
                    {
                        return found;
                    }
                }

                return null;
            }
            finally
            {
                ListPool<GameObject>.Release(roots);
            }
        }
    }
}
