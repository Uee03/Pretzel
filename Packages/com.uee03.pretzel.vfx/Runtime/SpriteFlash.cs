using UnityEngine;

namespace Pretzel.Vfx
{
    /// <summary>
    /// Flashes the tint of one or more sprites towards a colour and back. It only runs while a flash plays, restores the
    /// exact original colours when it ends or is disabled, and allocates nothing after the first use.
    /// A tint multiplies the sprite, so a flash toward white needs a shader with an additive or fill colour.
    /// </summary>
    [AddComponentMenu("Pretzel/VFX/Sprite Flash")]
    public sealed class SpriteFlash : MonoBehaviour
    {
        [Tooltip("The sprites to flash. Empty uses the SpriteRenderer on this object.")]
        [SerializeField] private SpriteRenderer[] _renderers;

        [SerializeField] private Color _color = new(1f, 0.25f, 0.2f, 1f);
        [SerializeField, Min(0.01f)] private float _duration = 0.15f;

        private Color[] _original;
        private Color _activeColor;
        private float _activeDuration;
        private float _elapsed;
        private bool _flashing;

        public bool IsFlashing => _flashing;

        /// <summary>Flashes with the colour and duration set in the Inspector.</summary>
        public void Flash()
        {
            Flash(_color, _duration);
        }

        public void Flash(Color color, float duration)
        {
            EnsureRenderers();
            if (_renderers.Length == 0)
            {
                return;
            }

            // Capture the colours only at the start of a flash, so a flash during a flash does not freeze the tint.
            if (!_flashing)
            {
                for (int i = 0; i < _renderers.Length; i++)
                {
                    _original[i] = _renderers[i] != null ? _renderers[i].color : Color.white;
                }
            }

            _activeColor = color;
            _activeDuration = Mathf.Max(0.01f, duration);
            _elapsed = 0f;
            _flashing = true;
            enabled = true;
            Apply(1f);
        }

        /// <summary>Ends the flash at once and restores the original colours.</summary>
        public void Stop()
        {
            if (_flashing)
            {
                Restore();
            }
        }

        private void Awake()
        {
            EnsureRenderers();
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

            Apply(FeelMath.FlashIntensity(_elapsed, _activeDuration));
        }

        private void OnDisable()
        {
            if (_flashing)
            {
                Restore();
            }
        }

        private void EnsureRenderers()
        {
            if (_renderers == null || _renderers.Length == 0)
            {
                SpriteRenderer own = GetComponent<SpriteRenderer>();
                _renderers = own != null ? new[] { own } : new SpriteRenderer[0];
            }

            if (_original == null || _original.Length != _renderers.Length)
            {
                _original = new Color[_renderers.Length];
            }
        }

        private void Apply(float intensity)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                {
                    _renderers[i].color = Color.Lerp(_original[i], _activeColor, intensity);
                }
            }
        }

        private void Restore()
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                {
                    _renderers[i].color = _original[i];
                }
            }

            _flashing = false;
            enabled = false;
        }
    }
}
