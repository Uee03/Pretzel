using UnityEngine;

namespace Pretzel.Core.Singletons
{
    /// <summary>
    /// Scene-bound singleton. Inherit with the derived type as the argument: <c>class Foo : Singleton&lt;Foo&gt;</c>.
    /// A second instance destroys its own component. Call base.Awake() when overriding Awake.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
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
                Debug.LogWarning($"Duplicate {typeof(T).Name} found, destroying the extra component.", this);
                Destroy(this);
                return;
            }

            _instance = (T)this;
        }
    }
}
