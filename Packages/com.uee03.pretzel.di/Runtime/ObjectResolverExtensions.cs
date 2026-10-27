using System.Collections.Generic;
using Pretzel.Core;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace Pretzel.DI
{
    public static class ObjectResolverExtensions
    {
        private static GameObject _inactiveHolder;

        public static T Resolve<T>(this IObjectResolver resolver) => (T)Guard.NotNull(resolver, nameof(resolver)).Resolve(typeof(T));

        public static bool TryResolve<T>(this IObjectResolver resolver, out T instance)
        {
            if (Guard.NotNull(resolver, nameof(resolver)).TryResolve(typeof(T), out object value))
            {
                instance = (T)value;
                return true;
            }

            instance = default;
            return false;
        }

        /// <summary>Fills [Inject] members of every MonoBehaviour on the object and its children, including inactive ones.</summary>
        public static void InjectGameObject(this IObjectResolver resolver, GameObject gameObject)
        {
            Guard.NotNull(resolver, nameof(resolver));
            Guard.NotDestroyed(gameObject, nameof(gameObject));
            List<MonoBehaviour> behaviours = ListPool<MonoBehaviour>.Get();
            try
            {
                gameObject.GetComponentsInChildren(true, behaviours);
                foreach (MonoBehaviour behaviour in behaviours)
                {
                    // A missing script shows up as a null entry.
                    if (behaviour)
                    {
                        resolver.Inject(behaviour);
                    }
                }
            }
            finally
            {
                ListPool<MonoBehaviour>.Release(behaviours);
            }
        }

        /// <summary>Instantiates a prefab and injects it before any of its Awake or OnEnable methods run.</summary>
        public static GameObject Instantiate(this IObjectResolver resolver, GameObject prefab, Transform parent = null)
        {
            Guard.NotDestroyed(prefab, nameof(prefab));
            return InstantiateInjected(resolver, prefab, parent, null, null);
        }

        public static GameObject Instantiate(this IObjectResolver resolver, GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            Guard.NotDestroyed(prefab, nameof(prefab));
            return InstantiateInjected(resolver, prefab, parent, position, rotation);
        }

        public static T Instantiate<T>(this IObjectResolver resolver, T prefab, Transform parent = null) where T : Component
        {
            Guard.NotDestroyed(prefab, nameof(prefab));
            return InstantiateInjected(resolver, prefab.gameObject, parent, null, null).GetComponent<T>();
        }

        public static T Instantiate<T>(this IObjectResolver resolver, T prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
        {
            Guard.NotDestroyed(prefab, nameof(prefab));
            return InstantiateInjected(resolver, prefab.gameObject, parent, position, rotation).GetComponent<T>();
        }

        // Instantiating under an inactive parent defers Awake and OnEnable until the instance is moved to its real parent.
        private static GameObject InstantiateInjected(IObjectResolver resolver, GameObject prefab, Transform parent, Vector3? position, Quaternion? rotation)
        {
            Guard.NotNull(resolver, nameof(resolver));
            if (!prefab.activeSelf)
            {
                GameObject inactive = Object.Instantiate(prefab, parent);
                Place(inactive.transform, position, rotation);
                resolver.InjectGameObject(inactive);
                return inactive;
            }

            GameObject instance = Object.Instantiate(prefab, GetInactiveHolder().transform, false);
            resolver.InjectGameObject(instance);
            instance.transform.SetParent(parent, false);
            if (parent == null)
            {
                SceneManager.MoveGameObjectToScene(instance, SceneManager.GetActiveScene());
            }

            Place(instance.transform, position, rotation);
            return instance;
        }

        private static void Place(Transform transform, Vector3? position, Quaternion? rotation)
        {
            if (position.HasValue && rotation.HasValue)
            {
                transform.SetPositionAndRotation(position.Value, rotation.Value);
            }
        }

        private static GameObject GetInactiveHolder()
        {
            if (_inactiveHolder)
            {
                return _inactiveHolder;
            }

            _inactiveHolder = new GameObject("[Pretzel.DI Instantiate]") { hideFlags = HideFlags.HideAndDontSave };
            _inactiveHolder.SetActive(false);
            if (Application.isPlaying)
            {
                Object.DontDestroyOnLoad(_inactiveHolder);
            }

            return _inactiveHolder;
        }
    }
}
