using UnityEngine;

namespace Pretzel.Core.Singletons
{
    /// <summary>
    /// Singleton that survives scene loads. The first instance wins and later duplicates destroy their GameObject.
    /// Inherit with the derived type as the argument: <c>class Foo : PersistentSingleton&lt;Foo&gt;</c>.
    /// Call base.Awake() when overriding Awake.
    /// </summary>
    public abstract class PersistentSingleton<T> : MonoBehaviour where T : PersistentSingleton<T>
    {
        [Tooltip("Move to the scene root on Awake. Required for DontDestroyOnLoad when this object has a parent.")]
        [SerializeField] private bool _autoUnparentOnAwake = true;

        private static T _instance;

        public static bool HasInstance => _instance;

        public static T TryGetInstance() => _instance ? _instance : null;

        /// <summary>
        /// The instance. One is created when playing if none exists, and null is returned in edit mode or while quitting.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = SingletonRuntime.FindOrCreate<T>();
                }

                return _instance;
            }
        }

        protected virtual void Awake()
        {
            InitializeSingleton();
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        protected virtual void InitializeSingleton()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (_instance && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (_autoUnparentOnAwake)
            {
                transform.SetParent(null);
            }

            _instance = (T)this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
