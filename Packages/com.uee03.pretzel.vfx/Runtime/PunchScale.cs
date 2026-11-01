using UnityEngine;

namespace Pretzel.Vfx
{
    /// <summary>
    /// A squash-and-stretch pop on an object's scale. It only runs while a punch plays, returns the exact original
    /// scale when it ends or is disabled, and allocates nothing. Runs on unscaled time so a hit stop does not freeze it.
    /// </summary>
    [AddComponentMenu("Pretzel/VFX/Punch Scale")]
    public sealed class PunchScale : MonoBehaviour
    {
        [Tooltip("How much bigger the object gets at the start, as a fraction of its scale. 0.25 is a quarter bigger.")]
        [SerializeField] private float _amount = 0.25f;

        [SerializeField, Min(0.01f)] private float _duration = 0.3f;

        [Tooltip("Oscillations before it settles.")]
        [SerializeField, Min(0.5f)] private float _cycles = 2f;

        private Vector3 _original;
        private float _activeAmount;
        private float _activeDuration;
        private float _elapsed;
        private bool _punching;

        public bool IsPunching => _punching;

        /// <summary>Punches with the amount and duration set in the Inspector.</summary>
        public void Punch()
        {
            Punch(_amount, _duration);
        }

        public void Punch(float amount, float duration)
        {
            // Capture the scale only at the start of a punch, so a punch during a punch does not keep the grown scale.
            if (!_punching)
            {
                _original = transform.localScale;
            }

            _activeAmount = amount;
            _activeDuration = Mathf.Max(0.01f, duration);
            _elapsed = 0f;
            _punching = true;
            enabled = true;
            Apply();
        }

        /// <summary>Ends the punch at once and restores the original scale.</summary>
        public void Stop()
        {
            if (_punching)
            {
                Restore();
            }
        }

        private void Awake()
        {
            enabled = false;
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed >= _activeDuration)
            {
                Restore();
                return;
            }

            Apply();
        }

        private void OnDisable()
        {
            if (_punching)
            {
                Restore();
            }
        }

        private void Apply()
        {
            transform.localScale = _original * (1f + FeelMath.Punch(_elapsed, _activeDuration, _activeAmount, _cycles));
        }

        private void Restore()
        {
            transform.localScale = _original;
            _punching = false;
            enabled = false;
        }
    }
}
