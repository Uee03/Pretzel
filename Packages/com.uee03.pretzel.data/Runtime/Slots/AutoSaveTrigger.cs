using System;
using UnityEngine;
using UnityEngine.Events;

namespace Pretzel.Data
{
    /// <summary>
    /// Raises <see cref="SaveRequested"/> on an interval and when the app is paused, loses focus or quits.
    /// It decides when to save; your save code decides what to save. Requests closer together than the minimum gap are dropped.
    /// </summary>
    [AddComponentMenu("Pretzel/Data/Auto Save Trigger")]
    public sealed class AutoSaveTrigger : MonoBehaviour
    {
        [Tooltip("Unscaled seconds between saves. 0 turns the interval off.")]
        [SerializeField, Min(0f)] private float _intervalSeconds = 120f;

        [Tooltip("Mobile apps are often killed after they are paused, so this is the most reliable save point there.")]
        [SerializeField] private bool _saveOnPause = true;

        [SerializeField] private bool _saveOnFocusLost;
        [SerializeField] private bool _saveOnQuit = true;
        [SerializeField, Min(0f)] private float _minimumGapSeconds = 5f;
        [SerializeField] private UnityEvent _onSave = new();

        private float _nextIntervalSave;
        private float _lastSave = float.NegativeInfinity;

        public event Action SaveRequested;

        public float IntervalSeconds
        {
            get => _intervalSeconds;
            set
            {
                _intervalSeconds = Mathf.Max(0f, value);
                _nextIntervalSave = Time.unscaledTime + _intervalSeconds;
            }
        }

        private void OnEnable()
        {
            _nextIntervalSave = Time.unscaledTime + _intervalSeconds;
        }

        private void Update()
        {
            if (_intervalSeconds > 0f && Time.unscaledTime >= _nextIntervalSave)
            {
                RequestSave();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _saveOnPause)
            {
                RequestSave(true);
            }
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && _saveOnFocusLost)
            {
                RequestSave();
            }
        }

        private void OnApplicationQuit()
        {
            if (_saveOnQuit)
            {
                RequestSave(true);
            }
        }

        /// <param name="force">Ignores the minimum gap.</param>
        public void RequestSave(bool force = false)
        {
            float now = Time.unscaledTime;
            _nextIntervalSave = now + _intervalSeconds;
            if (!force && now - _lastSave < _minimumGapSeconds)
            {
                return;
            }

            _lastSave = now;
            try
            {
                SaveRequested?.Invoke();
                _onSave.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
            }
        }
    }
}
