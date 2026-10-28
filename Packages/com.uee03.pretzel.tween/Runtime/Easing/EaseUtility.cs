using System.Runtime.CompilerServices;
using Pretzel.Core;
using UnityEngine;

namespace Pretzel.Tween
{
    /// <summary>
    /// Easing functions. Based on Kryzarel's gist (https://gist.github.com/Kryzarel/bba64622057f21a1d6d44879f9cd7bd4).
    /// Every function expects <c>time</c> in the range 0 to 1 and returns 0 at 0 and 1 at 1.
    /// <see cref="Evaluate"/> clamps <c>time</c> once. The individual functions do not, so clamp before calling them directly.
    /// </summary>
    public static class EaseUtility
    {
        private const float PI = Mathf.PI;
        private const float HalfPi = PI * 0.5f;
        private const float C1 = 1.70158f;
        private const float C2 = C1 + 1f;
        private const float C3 = C1 * 1.525f;

        #region EVALUATE

        /// <summary>Eases <paramref name="time"/> with the given curve.</summary>
        /// <param name="time">Normalized time, clamped to 0 to 1.</param>
        /// <param name="period">Elastic only. Lower values oscillate faster.</param>
        /// <param name="amplitude">Elastic only. Values below 1 are treated as 1.</param>
        public static float Evaluate(EaseType ease, float time, float period = 0.3f, float amplitude = 1f)
        {
            time = Mathf.Clamp01(time);
            return ease switch
            {
                EaseType.Linear => Linear(time),
                EaseType.InSine => InSine(time),
                EaseType.OutSine => OutSine(time),
                EaseType.InOutSine => InOutSine(time),
                EaseType.InQuad => InQuad(time),
                EaseType.OutQuad => OutQuad(time),
                EaseType.InOutQuad => InOutQuad(time),
                EaseType.InCubic => InCubic(time),
                EaseType.OutCubic => OutCubic(time),
                EaseType.InOutCubic => InOutCubic(time),
                EaseType.InQuart => InQuart(time),
                EaseType.OutQuart => OutQuart(time),
                EaseType.InOutQuart => InOutQuart(time),
                EaseType.InQuint => InQuint(time),
                EaseType.OutQuint => OutQuint(time),
                EaseType.InOutQuint => InOutQuint(time),
                EaseType.InExpo => InExpo(time),
                EaseType.OutExpo => OutExpo(time),
                EaseType.InOutExpo => InOutExpo(time),
                EaseType.InCirc => InCirc(time),
                EaseType.OutCirc => OutCirc(time),
                EaseType.InOutCirc => InOutCirc(time),
                EaseType.InBack => InBack(time),
                EaseType.OutBack => OutBack(time),
                EaseType.InOutBack => InOutBack(time),
                EaseType.InElastic => InElastic(time, amplitude, period),
                EaseType.OutElastic => OutElastic(time, amplitude, period),
                EaseType.InOutElastic => InOutElastic(time, amplitude, period),
                EaseType.InBounce => InBounce(time),
                EaseType.OutBounce => OutBounce(time),
                EaseType.InOutBounce => InOutBounce(time),
                _ => Linear(time)
            };
        }

        #endregion

        #region LERP

        // Lerp results are unclamped so overshoot curves (Back, Elastic) leave the from/to range.

        public static float Lerp(EaseType ease, float from, float to, float time, float period = 0.3f, float amplitude = 1f)
        {
            return Mathf.LerpUnclamped(from, to, Evaluate(ease, time, period, amplitude));
        }

        public static float Lerp(float from, float to, float time, AnimationCurve curve)
        {
            Guard.NotNull(curve, nameof(curve));
            return Mathf.LerpUnclamped(from, to, curve.Evaluate(time));
        }

        public static Vector2 Lerp(EaseType ease, Vector2 from, Vector2 to, float time, float period = 0.3f, float amplitude = 1f)
        {
            return Vector2.LerpUnclamped(from, to, Evaluate(ease, time, period, amplitude));
        }

        public static Vector2 Lerp(Vector2 from, Vector2 to, float time, AnimationCurve curve)
        {
            Guard.NotNull(curve, nameof(curve));
            return Vector2.LerpUnclamped(from, to, curve.Evaluate(time));
        }

        public static Vector3 Lerp(EaseType ease, Vector3 from, Vector3 to, float time, float period = 0.3f, float amplitude = 1f)
        {
            return Vector3.LerpUnclamped(from, to, Evaluate(ease, time, period, amplitude));
        }

        public static Vector3 Lerp(Vector3 from, Vector3 to, float time, AnimationCurve curve)
        {
            Guard.NotNull(curve, nameof(curve));
            return Vector3.LerpUnclamped(from, to, curve.Evaluate(time));
        }

        public static Color Lerp(EaseType ease, Color from, Color to, float time, float period = 0.3f, float amplitude = 1f)
        {
            return Color.LerpUnclamped(from, to, Evaluate(ease, time, period, amplitude));
        }

        public static Color Lerp(Color from, Color to, float time, AnimationCurve curve)
        {
            Guard.NotNull(curve, nameof(curve));
            return Color.LerpUnclamped(from, to, curve.Evaluate(time));
        }

        #endregion

        #region LINEAR

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Linear(float time) => time;

        #endregion

        #region SINE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InSine(float time) => 1f - Mathf.Cos(time * HalfPi);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float OutSine(float time) => Mathf.Sin(time * HalfPi);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InOutSine(float time) => -(Mathf.Cos(PI * time) - 1f) * 0.5f;

        #endregion

        #region QUAD

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InQuad(float time) => time * time;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float OutQuad(float time) => 1f - InQuad(1f - time);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InOutQuad(float time)
        {
            return time < 0.5f ? InQuad(time * 2f) * 0.5f : 1f - InQuad((1f - time) * 2f) * 0.5f;
        }

        #endregion

        #region CUBIC

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InCubic(float time) => time * time * time;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float OutCubic(float time) => 1f - InCubic(1f - time);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InOutCubic(float time)
        {
            return time < 0.5f ? InCubic(time * 2f) * 0.5f : 1f - InCubic((1f - time) * 2f) * 0.5f;
        }

        #endregion

        #region QUART

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InQuart(float time) => time * time * time * time;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float OutQuart(float time) => 1f - InQuart(1f - time);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InOutQuart(float time)
        {
            return time < 0.5f ? InQuart(time * 2f) * 0.5f : 1f - InQuart((1f - time) * 2f) * 0.5f;
        }

        #endregion

        #region QUINT

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InQuint(float time) => time * time * time * time * time;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float OutQuint(float time) => 1f - InQuint(1f - time);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InOutQuint(float time)
        {
            return time < 0.5f ? InQuint(time * 2f) * 0.5f : 1f - InQuint((1f - time) * 2f) * 0.5f;
        }

        #endregion

        #region EXPO

        // 2^(10 * (t - 1)) is about 0.001 at t = 0, so the endpoint is explicit.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InExpo(float time) => time <= 0f ? 0f : Mathf.Pow(2f, 10f * (time - 1f));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float OutExpo(float time) => 1f - InExpo(1f - time);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InOutExpo(float time)
        {
            return time < 0.5f ? InExpo(time * 2f) * 0.5f : 1f - InExpo((1f - time) * 2f) * 0.5f;
        }

        #endregion

        #region CIRC

        // Returns NaN for time outside 0 to 1. Evaluate clamps it.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InCirc(float time) => 1f - Mathf.Sqrt(1f - time * time);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float OutCirc(float time) => 1f - InCirc(1f - time);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InOutCirc(float time)
        {
            return time < 0.5f ? InCirc(time * 2f) * 0.5f : 1f - InCirc((1f - time) * 2f) * 0.5f;
        }

        #endregion

        #region BACK

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InBack(float time) => time * time * (C2 * time - C1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float OutBack(float time) => 1f - InBack(1f - time);

        // Standard 1.525 overshoot constant, so it matches easings.net and LitMotion.
        public static float InOutBack(float time)
        {
            if (time < 0.5f)
            {
                float t = time * 2f;
                return t * t * ((C3 + 1f) * t - C3) * 0.5f;
            }

            float u = time * 2f - 2f;
            return (u * u * ((C3 + 1f) * u + C3) + 2f) * 0.5f;
        }

        #endregion

        #region ELASTIC

        public static float InElastic(float time, float amplitude = 1f, float period = 0.3f)
        {
            return 1f - OutElastic(1f - time, amplitude, period);
        }

        public static float OutElastic(float time, float amplitude = 1f, float period = 0.3f)
        {
            if (time <= 0f)
            {
                return 0f;
            }

            if (time >= 1f)
            {
                return 1f;
            }

            // Penner's phase shift keeps the curve at 0 when time is 0, for any amplitude.
            amplitude = Mathf.Max(amplitude, 1f);
            period = Mathf.Max(period, 0.0001f);
            float shift = period / (2f * PI) * Mathf.Asin(1f / amplitude);
            return amplitude * Mathf.Pow(2f, -10f * time) * Mathf.Sin((time - shift) * (2f * PI) / period) + 1f;
        }

        public static float InOutElastic(float time, float amplitude = 1f, float period = 0.3f)
        {
            return time < 0.5f
                ? InElastic(time * 2f, amplitude, period) * 0.5f
                : 1f - InElastic((1f - time) * 2f, amplitude, period) * 0.5f;
        }

        #endregion

        #region BOUNCE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InBounce(float time) => 1f - OutBounce(1f - time);

        public static float OutBounce(float time)
        {
            const float divider = 2.75f;
            const float multiplier = 7.5625f;

            if (time < 1f / divider)
            {
                return multiplier * time * time;
            }

            if (time < 2f / divider)
            {
                time -= 1.5f / divider;
                return multiplier * time * time + 0.75f;
            }

            if (time < 2.5f / divider)
            {
                time -= 2.25f / divider;
                return multiplier * time * time + 0.9375f;
            }

            time -= 2.625f / divider;
            return multiplier * time * time + 0.984375f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InOutBounce(float time)
        {
            return time < 0.5f ? InBounce(time * 2f) * 0.5f : 1f - InBounce((1f - time) * 2f) * 0.5f;
        }

        #endregion
    }
}
