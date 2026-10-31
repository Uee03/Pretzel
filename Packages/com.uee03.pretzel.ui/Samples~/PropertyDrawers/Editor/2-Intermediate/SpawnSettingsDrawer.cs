using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.PropertyDrawers.Editors
{
    /// <summary>
    /// A type drawer for a [Serializable] class: a foldout with the child fields in two rows. PropertyFields keep binding,
    /// Undo and multi-object editing working, so the drawer only decides the layout.
    /// </summary>
    [CustomPropertyDrawer(typeof(SpawnSettings))]
    public sealed class SpawnSettingsDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new();
            root.AddToClassList("pretzel-drawer");
            SampleAssets.AddStyleSheet(root, "PropertyDrawerSamples.uss");

            Foldout foldout = new() { text = property.displayName, value = property.isExpanded };
            // The toggle inside the foldout raises its own change events that bubble up, so only react to the foldout itself.
            foldout.RegisterValueChangedCallback(evt =>
            {
                if (evt.target == foldout)
                {
                    property.isExpanded = evt.newValue;
                }
            });
            root.Add(foldout);

            VisualElement first = Row(foldout);
            first.Add(Cell(property, nameof(SpawnSettings.label), "Label"));
            first.Add(Cell(property, nameof(SpawnSettings.count), "Count"));

            VisualElement second = Row(foldout);
            second.Add(Cell(property, nameof(SpawnSettings.interval), "Interval"));
            second.Add(Cell(property, nameof(SpawnSettings.randomize), "Random"));
            return root;
        }

        private static VisualElement Row(VisualElement parent)
        {
            VisualElement row = new();
            row.AddToClassList("pretzel-drawer__row");
            parent.Add(row);
            return row;
        }

        private static PropertyField Cell(SerializedProperty parent, string relativePath, string label)
        {
            PropertyField field = new(parent.FindPropertyRelative(relativePath), label);
            field.AddToClassList("pretzel-drawer__cell");
            return field;
        }
    }
}
