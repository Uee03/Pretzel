using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.PropertyDrawers.Editors
{
    /// <summary>
    /// The smallest useful drawer. Wrapping the default field in a PropertyField keeps every built-in behaviour
    /// (undo, multi-object, prefab overrides, any property type); only the enabled state changes.
    /// </summary>
    [CustomPropertyDrawer(typeof(ReadOnlyFieldAttribute))]
    public sealed class ReadOnlyFieldDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            PropertyField field = new(property);
            field.SetEnabled(false);
            return field;
        }
    }
}
