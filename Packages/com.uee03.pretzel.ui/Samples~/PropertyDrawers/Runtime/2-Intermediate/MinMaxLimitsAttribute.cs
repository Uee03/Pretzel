using System;

namespace Pretzel.Samples.UI.PropertyDrawers
{
    /// <summary>Limits for a <see cref="FloatRange"/> field. A plain attribute: the type drawer reads it, no drawer is attached to it.</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MinMaxLimitsAttribute : Attribute
    {
        public MinMaxLimitsAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float Min { get; }

        public float Max { get; }
    }
}
