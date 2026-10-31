using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.PropertyDrawers.Editors
{
    /// <summary>
    /// A type drawer for <see cref="FloatRange"/>: two bound number fields around a MinMaxSlider. The slider has no binding of
    /// its own, so it writes both properties and follows them back with <c>TrackPropertyValue</c>.
    /// </summary>
    [CustomPropertyDrawer(typeof(FloatRange))]
    public sealed class FloatRangeDrawer : PropertyDrawer
    {
        private const float DefaultMin = 0f;
        private const float DefaultMax = 1f;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty min = property.FindPropertyRelative(nameof(FloatRange.min));
            SerializedProperty max = property.FindPropertyRelative(nameof(FloatRange.max));
            MinMaxLimitsAttribute limits = fieldInfo?.GetCustomAttribute<MinMaxLimitsAttribute>();

            VisualElement root = new();
            root.AddToClassList("pretzel-drawer");
            root.AddToClassList("pretzel-drawer__row");
            SampleAssets.AddStyleSheet(root, "PropertyDrawerSamples.uss");

            Label label = new(property.displayName);
            label.AddToClassList("unity-base-field__label");
            root.Add(label);

            FloatField minField = new();
            minField.AddToClassList("pretzel-range__field");
            minField.BindProperty(min);
            root.Add(minField);

            MinMaxSlider slider = new(min.floatValue, max.floatValue, limits?.Min ?? DefaultMin, limits?.Max ?? DefaultMax);
            slider.AddToClassList("pretzel-range__slider");
            slider.RegisterValueChangedCallback(evt => WriteRange(min, max, evt.newValue));
            root.Add(slider);

            FloatField maxField = new();
            maxField.AddToClassList("pretzel-range__field");
            maxField.BindProperty(max);
            root.Add(maxField);

            root.TrackPropertyValue(min, _ => SyncSlider(slider, min, max));
            root.TrackPropertyValue(max, _ => SyncSlider(slider, min, max));
            SyncSlider(slider, min, max);
            return root;
        }

        private static void WriteRange(SerializedProperty min, SerializedProperty max, Vector2 range)
        {
            SerializedObject serializedObject = min.serializedObject;
            serializedObject.Update();
            min.floatValue = range.x;
            max.floatValue = range.y;
            serializedObject.ApplyModifiedProperties();
        }

        // Writing both values through one slider would flatten different values on a multi-selection, so the slider is off then.
        private static void SyncSlider(MinMaxSlider slider, SerializedProperty min, SerializedProperty max)
        {
            slider.SetValueWithoutNotify(new Vector2(min.floatValue, max.floatValue));
            slider.SetEnabled(!min.hasMultipleDifferentValues && !max.hasMultipleDifferentValues);
        }
    }
}
