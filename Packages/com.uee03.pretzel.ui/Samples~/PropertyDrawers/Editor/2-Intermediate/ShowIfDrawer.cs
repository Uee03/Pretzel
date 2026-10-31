using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.PropertyDrawers.Editors
{
    /// <summary>
    /// Hides a field while a sibling bool is false. The drawer finds the condition next to the field (also inside nested
    /// classes and list elements) and follows it with <c>TrackPropertyValue</c>, which also fires on Undo.
    /// </summary>
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public sealed class ShowIfDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            ShowIfAttribute settings = (ShowIfAttribute)attribute;
            SerializedProperty condition = property.serializedObject.FindProperty(SiblingPath(property.propertyPath, settings.ConditionField));
            if (condition == null || condition.propertyType != SerializedPropertyType.Boolean)
            {
                return new HelpBox($"[ShowIf] needs a bool field named '{settings.ConditionField}' next to '{property.displayName}'.", HelpBoxMessageType.Error);
            }

            PropertyField field = new(property);
            Apply(field, condition, settings.Inverse);
            field.TrackPropertyValue(condition, changed => Apply(field, changed, settings.Inverse));
            return field;
        }

        private static string SiblingPath(string propertyPath, string siblingName)
        {
            int dot = propertyPath.LastIndexOf('.');
            return dot < 0 ? siblingName : propertyPath.Substring(0, dot + 1) + siblingName;
        }

        // With several objects selected, a mixed condition shows the field so nothing is hidden by accident.
        private static void Apply(VisualElement field, SerializedProperty condition, bool inverse)
        {
            bool visible = condition.hasMultipleDifferentValues || condition.boolValue != inverse;
            field.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
