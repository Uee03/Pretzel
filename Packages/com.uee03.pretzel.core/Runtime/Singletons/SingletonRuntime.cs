using UnityEngine;

namespace Pretzel.Core.Singletons
{
    internal static class SingletonRuntime
    {
        internal static bool IsQuitting { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            // Static state survives play sessions when domain reload is disabled.
            IsQuitting = false;
            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
        }

        private static void OnQuitting()
        {
            IsQuitting = true;
        }

        /// <summary>
        /// Find an existing instance, or create one when playing. Returns null in edit mode or while quitting if none exists.
        /// </summary>
        internal static T FindOrCreate<T>() where T : MonoBehaviour
        {
            T found = Object.FindAnyObjectByType<T>();
            if (found || !Application.isPlaying || IsQuitting)
            {
                return found;
            }

            GameObject go = new GameObject($"{typeof(T).Name} (Auto-Generated)");
            return go.AddComponent<T>();
        }
    }
}
