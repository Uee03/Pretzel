using Pretzel.UI.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.EditorUIHelpers
{
    /// <summary>An editor window built from EditorUI blocks: sections, help, bound property fields, a folder picker and buttons.</summary>
    public sealed class EditorUIHelpersWindow : EditorWindow
    {
        private ExportSettings _settings;

        [MenuItem("Window/Pretzel Samples/Editor UI Helpers")]
        private static void Open() => GetWindow<EditorUIHelpersWindow>("Editor UI Helpers");

        private void OnEnable()
        {
            _settings = CreateInstance<ExportSettings>();
            _settings.hideFlags = HideFlags.DontSave;
        }

        private void OnDisable()
        {
            if (_settings)
            {
                DestroyImmediate(_settings);
            }
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.style.paddingLeft = root.style.paddingRight = root.style.paddingTop = 6;
            EditorUI.ApplySkinClass(root);

            EditorUI.Header("Editor UI Helpers", root);
            EditorUI.Help("Every block below comes from Pretzel.UI.Editor.EditorUI.", parent: root);

            Foldout settings = EditorUI.Section("Settings", "pretzel-sample-settings", parent: root);
            SerializedObject serialized = new(_settings);
            settings.Add(EditorUI.PropertyFields(serialized, false, nameof(ExportSettings.outputFolder)));
            settings.Add(EditorUI.FolderField("Output Folder", _settings.outputFolder, path =>
            {
                Undo.RecordObject(_settings, "Output Folder");
                _settings.outputFolder = path;
            }));

            EditorUI.Separator(root);
            root.Add(EditorUI.ButtonRow(
                ("Log Settings", () => Debug.Log($"{_settings.outputFolder}/{_settings.fileName} x{_settings.scale}")),
                ("Inspect", () => Selection.activeObject = _settings)));
        }
    }
}
