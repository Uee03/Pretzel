using UnityEngine;

namespace Pretzel.Core.Singletons
{
    /// <summary>
    /// Persistent singleton where the newest instance wins: older instances of the same type are destroyed on Awake.
    /// Useful for objects that live in several scenes and should always reflect the latest scene's setup.
    /// Inherit with the derived type as the argument: <c>class Foo : RegulatorSingleton&lt;Foo&gt;</c>.
    /// Call base.Awake() when overriding Awake.
    /// </summary>
    public abstract class RegulatorSingleton<T> : MonoBehaviour where T : RegulatorSingleton<T>
    {
        private static T _instance;

        public static bool HasInstance => _instance;

        public static T TryGetInstance() => _instance ? _instance : null;

        public float InitializationTime { get; private set; }

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

            InitializationTime = Time.time;
            DontDestroyOnLoad(gameObject);

            // Every awake instance registers itself, so the only older live instance is the current one.
            if (_instance && _instance != this)
            {
                Destroy(_instance.gameObject);
            }

            _instance = (T)this;
        }
    }
}
