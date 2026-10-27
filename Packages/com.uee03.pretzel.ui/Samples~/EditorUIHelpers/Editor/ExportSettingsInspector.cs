using Pretzel.UI.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Pretzel.Samples.EditorUIHelpers
{
    /// <summary>An <see cref="InspectorBase"/> that keeps the default fields and adds a conditional one.</summary>
    [CustomEditor(typeof(ExportSettings))]
    public sealed class ExportSettingsInspector : InspectorBase
    {
        protected override void BuildInspector(VisualElement root)
        {
            EditorUI.Header("Export", root);
            AddDefaultProperties(root, nameof(ExportSettings.hiddenPrefix));

            PropertyField prefix = new(serializedObject.FindProperty(nameof(ExportSettings.hiddenPrefix)));
            prefix.Bind(serializedObject);
            root.Add(prefix);

            SerializedProperty includeHidden = serializedObject.FindProperty(nameof(ExportSettings.includeHidden));
            prefix.style.display = includeHidden.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
            root.TrackPropertyValue(includeHidden, p => prefix.style.display = p.boolValue ? DisplayStyle.Flex : DisplayStyle.None);
        }
    }
}
