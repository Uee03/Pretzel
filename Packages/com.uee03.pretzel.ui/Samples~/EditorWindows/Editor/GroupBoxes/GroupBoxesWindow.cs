using System.Collections.Generic;
using Pretzel.UI.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.EditorWindows
{
    /// <summary>UXML workflow: the layout and styles are assets, the window finds elements by name and wires events.</summary>
    public sealed class GroupBoxesWindow : EditorWindow
    {
        private const int MaxLogLines = 40;

        private readonly List<Foldout> _foldouts = new();
        private ScrollView _log;
        private Label _status;
        private int _logCount;

        [MenuItem("Pretzel/Samples/Editor Windows/Group Boxes")]
        private static void Open()
        {
            GroupBoxesWindow window = GetWindow<GroupBoxesWindow>();
            window.titleContent = new GUIContent("Group Boxes");
            window.minSize = new Vector2(340f, 420f);
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.AddToClassList("pretzel-sample");
            EditorUI.ApplySkinClass(root);
            SampleAssets.AddStyleSheets(root, "SampleWindows.uss", "GroupBoxesWindow.uss");
            if (!SampleAssets.CloneLayout(root, "GroupBoxesWindow.uxml"))
            {
                return;
            }

            _log = root.Q<ScrollView>("groupboxes__log");
            _status = root.Q<Label>("groupboxes__status");
            _logCount = 0;

            // Radio buttons in one GroupBox are exclusive; each one raises its own change event.
            GroupBox quality = root.Q<GroupBox>("groupboxes__quality");
            quality.Query<RadioButton>().ForEach(RegisterRadioButton);

            RadioButtonGroup detail = root.Q<RadioButtonGroup>("groupboxes__detail");
            detail.RegisterValueChangedCallback(OnDetailChanged);

            root.Q<Toggle>("groupboxes__shadows").RegisterValueChangedCallback(OnShadowsChanged);

            _foldouts.Clear();
            root.Query<Foldout>().ToList(_foldouts);
            root.Q<Button>("groupboxes__expand").clicked += ExpandAll;
            root.Q<Button>("groupboxes__collapse").clicked += CollapseAll;

            Log("Window created");
        }

        private void RegisterRadioButton(RadioButton button)
        {
            button.RegisterValueChangedCallback(OnQualityChanged);
        }

        private void OnQualityChanged(ChangeEvent<bool> evt)
        {
            if (evt.newValue && evt.target is RadioButton button)
            {
                Log("Quality: " + button.label);
            }
        }

        private void OnDetailChanged(ChangeEvent<int> evt)
        {
            Log("Detail index: " + evt.newValue);
        }

        private void OnShadowsChanged(ChangeEvent<bool> evt)
        {
            Log("Shadows: " + evt.newValue);
        }

        private void ExpandAll()
        {
            SetAllFoldouts(true);
        }

        private void CollapseAll()
        {
            SetAllFoldouts(false);
        }

        private void SetAllFoldouts(bool open)
        {
            for (int i = 0; i < _foldouts.Count; i++)
            {
                _foldouts[i].value = open;
            }

            Log(open ? "Expanded all foldouts" : "Collapsed all foldouts");
        }

        private void Log(string message)
        {
            Label line = new(message);
            line.AddToClassList("pretzel-groupboxes__log-line");
            _log.Add(line);
            _logCount++;
            if (_logCount > MaxLogLines)
            {
                _log.contentContainer[0].RemoveFromHierarchy();
                _logCount--;
            }

            _log.ScrollTo(line);
            _status.text = message;
        }
    }
}
