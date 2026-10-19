using UnityEngine;

namespace Pretzel.Extensions
{
    public static class FloatExtensions
    {
        /// <summary>
        /// Format seconds as mm:ss. Minutes keep counting past 59, so 3725 seconds is "62:05".
        /// Negative and non-finite values are treated as zero.
        /// </summary>
        public static string ToTimeMS(this float value)
        {
            int total = ToWholeSeconds(value);
            return $"{total / 60:00}:{total % 60:00}";
        }

        /// <summary>
        /// Format seconds as hh:mm:ss. Hours keep counting past 23, so 90000 seconds is "25:00:00".
        /// Negative and non-finite values are treated as zero.
        /// </summary>
        public static string ToTimeHMS(this float value)
        {
            int total = ToWholeSeconds(value);
            return $"{total / 3600:00}:{total % 3600 / 60:00}:{total % 60:00}";
        }

        private static int ToWholeSeconds(float value)
        {
            return float.IsFinite(value) && value > 0f ? (int)Mathf.Min(value, 2_000_000_000f) : 0;
        }
    }
}
