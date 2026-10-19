using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Data.Editor
{
    [CustomPropertyDrawer(typeof(SerializableGuid))]
    public class SerializableGuidDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty guid = property.FindPropertyRelative(SerializableGuid.GuidFieldName);
            if (guid == null)
            {
                return new HelpBox($"{property.displayName}: SerializableGuid has no '{SerializableGuid.GuidFieldName}' field.", HelpBoxMessageType.Error);
            }

            VisualElement root = new();
            root.AddToClassList("pretzel-guid");
            root.styleSheets.Add(DataEditorAssets.Style("SerializableGuidDrawer"));

            TextField field = new(property.displayName) { isReadOnly = true };
            field.AddToClassList(TextField.alignedFieldUssClassName);
            field.AddToClassList("pretzel-guid__field");
            field.BindProperty(guid);
            root.Add(field);

            root.Add(CreateButton("Refresh", "Regenerate", "Generate a new GUID", () =>
            {
                bool hasValue = guid.stringValue != SerializableGuid.EmptyGuidString();
                if (hasValue && !EditorUtility.DisplayDialog("Regenerate GUID", "Replace the current GUID?", "Replace", "Cancel"))
                {
                    return;
                }

                SetGuid(guid, SerializableGuid.NewGuidString());
            }));
            root.Add(CreateButton("TreeEditor.Trash", "Clear", "Set the GUID to empty", () => SetGuid(guid, SerializableGuid.EmptyGuidString())));

            return root;
        }

        // Going through the SerializedProperty keeps undo, prefab overrides and multi-edit working.
        private static void SetGuid(SerializedProperty guid, string value)
        {
            guid.stringValue = value;
            guid.serializedObject.ApplyModifiedProperties();
        }

        private static Button CreateButton(string iconName, string fallbackText, string tooltip, System.Action onClick)
        {
            Button button = new(onClick) { tooltip = tooltip };
            Texture icon = EditorGUIUtility.IconContent(iconName).image;
            if (icon)
            {
                Image image = new() { image = icon };
                image.AddToClassList("pretzel-guid__icon");
                button.Add(image);
            }
            else
            {
                button.text = fallbackText;
            }

            return button;
        }
    }
}
