using UnityEngine;

namespace Pretzel.Samples.UI.PropertyDrawers
{
    /// <summary>Draws a float or int field with a progress bar between <see cref="Min"/> and <see cref="Max"/>.</summary>
    public sealed class ProgressBarAttribute : PropertyAttribute
    {
        public ProgressBarAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float Min { get; }

        public float Max { get; }
    }
}
