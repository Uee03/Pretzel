using Pretzel.Tween;
using UnityEngine;

namespace Pretzel.Samples.Tween
{
    /// <summary>Moves a transform with an ease type, then pulses its scale with an AnimationCurve.</summary>
    public class TweenUsageExample : MonoBehaviour
    {
        [SerializeField] private EaseType _ease = EaseType.OutBack;
        [SerializeField] private Vector3 _target = new(3f, 0f, 0f);
        [SerializeField] private float _duration = 1.5f;
        [SerializeField] private AnimationCurve _pulse = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Vector3 _start;
        private float _elapsed;

        private void Start()
        {
            _start = transform.position;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.PingPong(_elapsed, _duration) / _duration;

            // Lerp is unclamped, so OutBack and the elastic curves overshoot the target.
            transform.position = EaseUtility.Lerp(_ease, _start, _target, t);
            transform.localScale = Vector3.one * EaseUtility.Lerp(1f, 1.5f, t, _pulse);
        }
    }
}
