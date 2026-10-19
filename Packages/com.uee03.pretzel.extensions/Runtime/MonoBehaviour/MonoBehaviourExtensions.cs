using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pretzel.Extensions
{
    public static class MonoBehaviourExtensions
    {
        /// <summary>
        /// Log a warning that highlights this behaviour when clicked in the Console.
        /// </summary>
        public static void PrintWarning(this MonoBehaviour behaviour, string message)
        {
            Debug.LogWarning(message, behaviour);
        }

        public static void PrintError(this MonoBehaviour behaviour, string message)
        {
            Debug.LogError(message, behaviour);
        }

        public static void PrintException(this MonoBehaviour behaviour, Exception exception)
        {
            Debug.LogException(exception, behaviour);
        }

        public static void ActivateGO(this MonoBehaviour behaviour)
        {
            behaviour.gameObject.Activate();
        }

        public static void DeactivateGO(this MonoBehaviour behaviour)
        {
            behaviour.gameObject.Deactivate();
        }

        /// <summary>
        /// Deactivate the GameObject after the given scaled seconds. The behaviour must be active to run the timer.
        /// </summary>
        public static void DeactivateGO(this MonoBehaviour behaviour, float seconds)
        {
            behaviour.StartCoroutine(behaviour.gameObject.DeactivateRoutine(seconds));
        }

        public static void DestroyGO(this MonoBehaviour behaviour)
        {
            behaviour.gameObject.Destroy();
        }

        public static void DestroyGO(this MonoBehaviour behaviour, float seconds)
        {
            behaviour.gameObject.Destroy(seconds);
        }

        /// <summary>
        /// Enable this behaviour.
        /// </summary>
        public static void Activate(this MonoBehaviour behaviour)
        {
            behaviour.enabled = true;
        }

        /// <summary>
        /// Disable this behaviour.
        /// </summary>
        public static void Deactivate(this MonoBehaviour behaviour)
        {
            behaviour.enabled = false;
        }

        /// <summary>
        /// Disable this behaviour after the given scaled seconds. The behaviour must be active to run the timer.
        /// </summary>
        public static void Deactivate(this MonoBehaviour behaviour, float seconds)
        {
            behaviour.StartCoroutine(DisableAfter(behaviour, seconds));
        }

        /// <summary>
        /// Destroy this component only, not its GameObject.
        /// </summary>
        public static void Destroy(this MonoBehaviour behaviour)
        {
            Object.Destroy(behaviour);
        }

        public static void Destroy(this MonoBehaviour behaviour, float seconds)
        {
            Object.Destroy(behaviour, seconds);
        }

        public static void SetParent(this MonoBehaviour behaviour, Transform parent)
        {
            behaviour.transform.SetParent(parent);
        }

        private static IEnumerator DisableAfter(MonoBehaviour behaviour, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (behaviour)
            {
                behaviour.enabled = false;
            }
        }
    }
}
