using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.PropertyDrawers.Editors
{
    /// <summary>
    /// An attribute drawer with a parameter: the default editable field plus a ProgressBar that follows it.
    /// <c>TrackPropertyValue</c> keeps the bar current after edits, Undo and multi-object changes.
    /// </summary>
    [CustomPropertyDrawer(typeof(ProgressBarAttribute))]
    public sealed class ProgressBarDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.Float && property.propertyType != SerializedPropertyType.Integer)
            {
                return new HelpBox("[ProgressBar] supports float and int fields.", HelpBoxMessageType.Error);
            }

            ProgressBarAttribute settings = (ProgressBarAttribute)attribute;

            VisualElement root = new();
            root.AddToClassList("pretzel-progress");
            root.Add(new PropertyField(property));

            ProgressBar bar = new() { lowValue = settings.Min, highValue = settings.Max };
            bar.AddToClassList("pretzel-progress__bar");
            root.Add(bar);

            root.TrackPropertyValue(property, changed => UpdateBar(bar, changed));
            UpdateBar(bar, property);
            return root;
        }

        private static void UpdateBar(ProgressBar bar, SerializedProperty property)
        {
            float value = property.propertyType == SerializedPropertyType.Float ? property.floatValue : property.intValue;
            bar.value = Mathf.Clamp(value, bar.lowValue, bar.highValue);
            bar.title = property.hasMultipleDifferentValues ? "-" : value.ToString("0.##");
        }
    }
}
