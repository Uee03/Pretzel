using UnityEngine;

namespace Pretzel.Vfx
{
    /// <summary>The curves behind the effects, as pure functions of normalized time. Time 0 is the start and 1 is the end.</summary>
    public static class FeelMath
    {
        /// <summary>1 at the start, easing out to exactly 0 at the end.</summary>
        public static float FlashIntensity(float elapsed, float duration)
        {
            float remaining = 1f - Progress(elapsed, duration);
            return remaining * remaining;
        }

        /// <summary>
        /// A punch: <paramref name="amplitude"/> at the start, swinging through <paramref name="cycles"/> oscillations and
        /// settling to exactly 0 at the end. Use it as a scale offset.
        /// </summary>
        public static float Punch(float elapsed, float duration, float amplitude, float cycles = 2f)
        {
            float t = Progress(elapsed, duration);
            float envelope = (1f - t) * (1f - t);
            return amplitude * Mathf.Cos(2f * Mathf.PI * cycles * t) * envelope;
        }

        public static float Progress(float elapsed, float duration)
        {
            return duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
        }
    }
}
