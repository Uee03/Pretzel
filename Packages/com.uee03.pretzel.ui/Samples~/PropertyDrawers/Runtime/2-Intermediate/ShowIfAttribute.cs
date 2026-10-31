using UnityEngine;

namespace Pretzel.Samples.UI.PropertyDrawers
{
    /// <summary>Shows the field only while the named bool field on the same object (or nested object) is true.</summary>
    public sealed class ShowIfAttribute : PropertyAttribute
    {
        public ShowIfAttribute(string conditionField, bool inverse = false)
        {
            ConditionField = conditionField;
            Inverse = inverse;
        }

        public string ConditionField { get; }

        public bool Inverse { get; }
    }
}
