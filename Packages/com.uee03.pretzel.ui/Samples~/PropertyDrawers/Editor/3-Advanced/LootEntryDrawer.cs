using UnityEditor;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.PropertyDrawers.Editors
{
    /// <summary>A drawer for a serializable type that is used in a list: each element is drawn as a <see cref="LootEntryElement"/>.</summary>
    [CustomPropertyDrawer(typeof(LootEntry))]
    public sealed class LootEntryDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new LootEntryElement(property);
        }
    }
}
