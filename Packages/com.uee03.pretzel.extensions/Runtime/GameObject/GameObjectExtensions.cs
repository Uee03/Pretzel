using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pretzel.Extensions
{
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Get the parent Transform, or this object's own Transform when it has no parent.
        /// </summary>
        public static Transform GetParent(this GameObject gameObject)
        {
            Transform transform = gameObject.transform;
            return transform.parent ? transform.parent : transform;
        }

        /// <summary>
        /// Get the parent GameObject, or this object when it has no parent.
        /// </summary>
        public static GameObject GetParentGo(this GameObject gameObject)
        {
            return gameObject.GetParent().gameObject;
        }

        public static void SetParent(this GameObject gameObject, Transform parent)
        {
            gameObject.transform.SetParent(parent);
        }

        /// <summary>
        /// Set the parent. A null or destroyed parent moves this object to the scene root.
        /// </summary>
        public static void SetParent(this GameObject gameObject, GameObject parent)
        {
            gameObject.transform.SetParent(parent ? parent.transform : null);
        }

        public static void Activate(this GameObject gameObject)
        {
            gameObject.SetActive(true);
        }

        public static void Deactivate(this GameObject gameObject)
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Coroutine that activates the GameObject after the given scaled seconds. Start it with StartCoroutine.
        /// </summary>
        public static IEnumerator ActivateRoutine(this GameObject gameObject, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (gameObject)
            {
                gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Coroutine that deactivates the GameObject after the given scaled seconds. Start it with StartCoroutine.
        /// </summary>
        public static IEnumerator DeactivateRoutine(this GameObject gameObject, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (gameObject)
            {
                gameObject.SetActive(false);
            }
        }

        public static void Destroy(this GameObject gameObject)
        {
            Object.Destroy(gameObject);
        }

        public static void Destroy(this GameObject gameObject, float seconds)
        {
            Object.Destroy(gameObject, seconds);
        }

        /// <summary>
        /// Instantiate a copy of this GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to copy</param>
        /// <param name="position">World position of the copy, defaults to the origin</param>
        /// <param name="rotation">World rotation of the copy, defaults to no rotation</param>
        /// <param name="parent">Optional parent of the copy</param>
        public static GameObject Clone(this GameObject gameObject, Vector3 position = default, Quaternion? rotation = null, Transform parent = null)
        {
            return Object.Instantiate(gameObject, position, rotation ?? Quaternion.identity, parent);
        }
    }
}
