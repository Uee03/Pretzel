using UnityEngine;

namespace Pretzel.Audio
{
    public static class AudioMath
    {
        /// <summary>The level an AudioMixer treats as silence.</summary>
        public const float MinDecibels = -80f;

        /// <summary>Converts a 0 to 1 slider value to decibels for an AudioMixer parameter.</summary>
        public static float LinearToDecibels(float linear)
        {
            return linear <= 0.0001f ? MinDecibels : Mathf.Max(MinDecibels, 20f * Mathf.Log10(linear));
        }

        public static float DecibelsToLinear(float decibels)
        {
            return decibels <= MinDecibels ? 0f : Mathf.Pow(10f, decibels / 20f);
        }
    }
}
