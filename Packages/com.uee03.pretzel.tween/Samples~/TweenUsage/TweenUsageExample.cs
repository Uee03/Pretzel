using System;
using Pretzel.Tween;
using UnityEngine;

namespace Pretzel.Samples.Tween
{
    /// <summary>
    /// Add to a visible object (a cube works) and press Play. It moves with an ease type, pulses its scale with an
    /// AnimationCurve, squashes with a Vector2 lerp and tints its renderer with a Color lerp.
    /// </summary>
    public class TweenUsageExample : MonoBehaviour
    {
        private static readonly int _colorId = Shader.PropertyToID("_Color");
        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");

        [Header("Movement")]
        [SerializeField] private EaseType _ease = EaseType.OutBack;
        [SerializeField] private Vector3 _offset = new(3f, 0f, 0f);
        [SerializeField, Min(0.01f)] private float _duration = 1.5f;

        [Header("Elastic")]
        [Tooltip("Lower values oscillate faster.")]
        [SerializeField, Min(0.01f)] private float _period = 0.3f;
        [Tooltip("Wave height. Values below 1 are treated as 1.")]
        [SerializeField, Min(1f)] private float _amplitude = 1f;

        [Header("Scale, Squash and Colour")]
        [Tooltip("Drives every Lerp with the Pulse curve instead of an EaseType.")]
        [SerializeField] private bool _useCurve;
        [SerializeField] private AnimationCurve _pulse = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private Vector2 _squash = new(1.2f, 0.8f);
        [SerializeField] private Color _fromColor = Color.white;
        [SerializeField] private Color _toColor = new(1f, 0.6f, 0.1f);

        private Vector3 _start;
        private Vector3 _baseScale;
        private float _elapsed;
        private Renderer _renderer;
        private MaterialPropertyBlock _block;
        private int _colorProperty = -1;

        private void Start()
        {
            _start = transform.position;
            _baseScale = transform.localScale;

            // A property block tints this renderer without creating a material copy.
            if (TryGetComponent(out _renderer) && _renderer.sharedMaterial)
            {
                Material material = _renderer.sharedMaterial;
                _colorProperty = material.HasProperty(_baseColorId) ? _baseColorId : material.HasProperty(_colorId) ? _colorId : -1;
                _block = new MaterialPropertyBlock();
            }

            LogEaseTable();
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.PingPong(_elapsed, _duration) / _duration;

            // Lerp is unclamped, so Back and Elastic overshoot the target. Period and amplitude only affect Elastic.
            // Every Lerp has an AnimationCurve overload for shapes an EaseType cannot express.
            Vector3 target = _start + _offset;
            transform.position = _useCurve
                ? EaseUtility.Lerp(_start, target, t, _pulse)
                : EaseUtility.Lerp(_ease, _start, target, t, _period, _amplitude);

            float pulse = EaseUtility.Lerp(1f, 1.25f, t, _pulse);
            Vector2 squash = _useCurve
                ? EaseUtility.Lerp(Vector2.one, _squash, t, _pulse)
                : EaseUtility.Lerp(EaseType.InOutSine, Vector2.one, _squash, t);
            transform.localScale = Vector3.Scale(_baseScale, new Vector3(squash.x, squash.y, 1f) * pulse);

            if (_colorProperty >= 0)
            {
                Color color = _useCurve
                    ? EaseUtility.Lerp(_fromColor, _toColor, t, _pulse)
                    : EaseUtility.Lerp(EaseType.InOutQuad, _fromColor, _toColor, t);
                _renderer.GetPropertyBlock(_block);
                _block.SetColor(_colorProperty, color);
                _renderer.SetPropertyBlock(_block);
            }
        }

        private void OnDestroy()
        {
            if (_renderer && _block != null)
            {
                _renderer.SetPropertyBlock(null);
            }
        }

        // Evaluate turns any EaseType into a 0..1 progress value, which you can feed into your own maths.
        private void LogEaseTable()
        {
            foreach (EaseType ease in (EaseType[])Enum.GetValues(typeof(EaseType)))
            {
                Debug.Log($"{ease,-14} t=0.25 {EaseUtility.Evaluate(ease, 0.25f):0.000}  t=0.5 {EaseUtility.Evaluate(ease, 0.5f):0.000}  t=0.75 {EaseUtility.Evaluate(ease, 0.75f):0.000}", this);
            }

            // The individual functions skip the switch. Note that they take amplitude before period.
            Debug.Log($"OutBounce(0.5) = {EaseUtility.OutBounce(0.5f):0.000}, OutElastic(0.5, {_amplitude}, {_period}) = {EaseUtility.OutElastic(0.5f, _amplitude, _period):0.000}", this);
        }
    }
}
