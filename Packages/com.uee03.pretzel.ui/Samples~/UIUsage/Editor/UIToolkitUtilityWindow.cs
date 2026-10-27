using System;
using Pretzel.UI;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.EditorWindows
{
    /// <summary>The Editor-only factories of <see cref="UIToolkitUtility"/>: toolbar items, Editor fields and a bound PropertyField.</summary>
    public sealed class UIToolkitUtilityWindow : EditorWindow
    {
        [Flags]
        private enum Features
        {
            None = 0,
            Audio = 1,
            Physics = 2,
            Network = 4
        }

        [SerializeField] private string _note = "Bound with CreatePropertyField.";

        private SerializedObject _serializedWindow;

        [MenuItem("Tools/Pretzel Samples/UI Toolkit Utility Window")]
        private static void Open() => GetWindow<UIToolkitUtilityWindow>("UI Toolkit Utility");

        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            Toolbar toolbar = UIToolkitUtility.CreateToolbar(root);
            ToolbarButton refresh = UIToolkitUtility.CreateToolbarButton(toolbar);
            refresh.text = "Log";
            refresh.clicked += () => Debug.Log("Toolbar button clicked.");
            ToolbarMenu menu = UIToolkitUtility.CreateToolbarMenu(toolbar);
            menu.text = "Menu";
            menu.menu.AppendAction("Say hello", _ => Debug.Log("Hello from the toolbar menu."));
            UIToolkitUtility.CreateToolbarToggle(toolbar).text = "Toggle";
            UIToolkitUtility.CreateToolbarSpacer(toolbar).style.flexGrow = 1f;
            UIToolkitUtility.CreateToolbarSearchField(toolbar);
            UIToolkitUtility.CreateToolbarPopupSearchField(toolbar);

            VisualElement body = UIToolkitUtility.CreateContainer(root);
            body.style.paddingLeft = body.style.paddingRight = body.style.paddingTop = 6f;

            UIToolkitUtility.CreateTagField("Tag", body).value = "Untagged";
            UIToolkitUtility.CreateLayerField("Layer", body);
            MaskField mask = UIToolkitUtility.CreateMaskField("Channels", body);
            mask.choices = new() { "Red", "Green", "Blue" };
            UIToolkitUtility.CreateEnumFlagsField("Features", Features.Audio | Features.Network, body);
            UIToolkitUtility.CreateObjectField("Prefab", false, typeof(GameObject), body);

            // PropertyField needs a SerializedProperty. Here it edits a field of this window, with undo.
            _serializedWindow = new SerializedObject(this);
            UIToolkitUtility.CreatePropertyField("Note", _serializedWindow.FindProperty(nameof(_note)), body);
        }

        private void OnDestroy()
        {
            _serializedWindow?.Dispose();
        }
    }
}
