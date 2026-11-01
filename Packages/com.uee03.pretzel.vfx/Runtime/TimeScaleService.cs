using UnityEngine;

namespace Pretzel.Vfx
{
    /// <summary>
    /// Applies a <see cref="TimeScaleStack"/> to <see cref="Time.timeScale"/>. If other code changes the time scale
    /// meanwhile, for example a pause menu, that value becomes the new baseline, so a hit stop never undoes a pause and
    /// the time always returns to what it was when the last request ends.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class TimeScaleService : MonoBehaviour, ITimeScale
    {
        /// <summary>
        /// A frozen game runs at this scale, not at exactly 0. It looks and behaves frozen, and it lets the service tell a
        /// hit stop from a pause that another system sets to 0 meanwhile, so it never undoes that pause.
        /// </summary>
        public const float MinimumScale = 0.0001f;

        private static TimeScaleService _instance;

        private readonly TimeScaleStack _stack = new();
        private float _baseline = 1f;
        private float _applied = 1f;
        private bool _owning;

        /// <summary>The shared service, created on first use and kept for the session.</summary>
        public static TimeScaleService Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject host = new("[Pretzel Time Scale]") { hideFlags = HideFlags.HideInHierarchy };
                    _instance = host.AddComponent<TimeScaleService>();
                    if (Application.isPlaying)
                    {
                        DontDestroyOnLoad(host);
                    }
                }

                return _instance;
            }
        }

        public TimeScaleStack Stack => _stack;

        public int Freeze(float seconds)
        {
            return SlowMotion(0f, seconds);
        }

        public int SlowMotion(float scale, float seconds)
        {
            int id = _stack.Push(scale, seconds);
            if (id != 0)
            {
                Apply();
            }

            return id;
        }

        public bool Cancel(int id)
        {
            bool cancelled = _stack.Cancel(id);
            Apply();
            return cancelled;
        }

        /// <summary>Drops every request and gives the time back at once.</summary>
        public void CancelAll()
        {
            _stack.Clear();
            Apply();
        }

        // Domain reload may be off, so a stale instance from the last session must not survive into the next.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
        }

        private void Update()
        {
            if (_stack.IsActive)
            {
                _stack.Advance(Time.unscaledDeltaTime);
                Apply();
            }
        }

        private void OnDestroy()
        {
            if (_owning && Mathf.Approximately(Time.timeScale, _applied))
            {
                Time.timeScale = _baseline;
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Apply()
        {
            if (_stack.IsActive)
            {
                if (!_owning || !Mathf.Approximately(Time.timeScale, _applied))
                {
                    _baseline = Time.timeScale;
                    _owning = true;
                }

                _applied = _baseline > 0f ? Mathf.Max(MinimumScale, _baseline * _stack.CurrentScale) : 0f;
                Time.timeScale = _applied;
            }
            else if (_owning)
            {
                if (Mathf.Approximately(Time.timeScale, _applied))
                {
                    Time.timeScale = _baseline;
                }

                _owning = false;
            }
        }
    }
}
