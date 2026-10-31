using System;

namespace Pretzel.Samples.UI.PropertyDrawers
{
    /// <summary>A minimum and maximum pair, drawn as a slider with two number fields.</summary>
    [Serializable]
    public sealed class FloatRange
    {
        public float min;
        public float max;

        public FloatRange()
        {
        }

        public FloatRange(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }
}
