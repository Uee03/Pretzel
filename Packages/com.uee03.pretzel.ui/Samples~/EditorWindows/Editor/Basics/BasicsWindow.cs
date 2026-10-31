using Pretzel.UI.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.EditorWindows
{
    /// <summary>
    /// Code workflow: <c>CreateGUI</c>, toolbar, fields, buttons, help boxes and three ways to persist state.
    /// Everything is built in C#; the shared USS only adds spacing.
    /// </summary>
    public sealed class BasicsWindow : EditorWindow
    {
        private const string CounterKey = "Pretzel.Samples.UI.Basics.Counter";
        private const string UserNameKey = "Pretzel.Samples.UI.Basics.UserName";
        private const string ShowTipsKey = "Pretzel.Samples.UI.Basics.ShowTips";
        private const long SaveDelayMilliseconds = 500;

        private SerializedObject _settings;
        private IVisualElementScheduledItem _saveTask;
        private bool _savePending;
        private Label _selectionLabel;
        private HelpBox _tips;
        private HelpBox _pickHelp;
        private ObjectField _pickField;

        [MenuItem("Pretzel/Samples/Editor Windows/Basics")]
        private static void Open()
        {
            BasicsWindow window = GetWindow<BasicsWindow>();
            window.titleContent = new GUIContent("Basics");
            window.minSize = new Vector2(360f, 460f);
        }

        // Static events outlive the window, so subscribe in OnEnable and unsubscribe in OnDisable.
        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            if (_savePending && BasicsSettings.instance)
            {
                BasicsSettings.instance.SaveSettings();
            }

            _savePending = false;
            _settings?.Dispose();
            _settings = null;
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.AddToClassList("pretzel-sample");
            EditorUI.ApplySkinClass(root);
            SampleAssets.AddStyleSheets(root, "SampleWindows.uss");

            root.Add(BuildToolbar());

            ScrollView body = new();
            body.AddToClassList("pretzel-sample__body");
            root.Add(body);

            Label title = new("Editor window basics");
            title.AddToClassList("pretzel-sample__title");
            body.Add(title);

            _tips = EditorUI.Help("Each section stores its value differently. Change a value, then recompile or restart the Editor to see what survives.", parent: body);
            _tips.style.display = EditorPrefs.GetBool(ShowTipsKey, true) ? DisplayStyle.Flex : DisplayStyle.None;

            BuildSessionSection(body);
            BuildPrefsSection(body);
            BuildSingletonSection(body, root);
            BuildFieldsSection(body);
        }

        private VisualElement BuildToolbar()
        {
            Toolbar toolbar = new();
            toolbar.Add(new ToolbarButton(LogState) { text = "Log State" });
            toolbar.Add(new ToolbarButton(ResetState) { text = "Reset" });
            toolbar.Add(new ToolbarSpacer { flex = true });

            ToolbarToggle tips = new() { text = "Tips", value = EditorPrefs.GetBool(ShowTipsKey, true) };
            tips.RegisterValueChangedCallback(OnTipsToggled);
            toolbar.Add(tips);
            return toolbar;
        }

        private static void BuildSessionSection(VisualElement body)
        {
            Foldout section = EditorUI.Section("SessionState: survives a domain reload", "pretzel-basics-session", parent: body);
            EditorUI.Help("Cleared when the Editor closes. Good for transient UI state.", parent: section);

            IntegerField counter = new("Counter") { value = SessionState.GetInt(CounterKey, 0), isDelayed = true };
            counter.RegisterValueChangedCallback(OnCounterChanged);
            section.Add(EditorUI.Row(counter, EditorUI.Button("+1", () => counter.value += 1)));
        }

        private static void BuildPrefsSection(VisualElement body)
        {
            Foldout section = EditorUI.Section("EditorPrefs: per user, all projects", "pretzel-basics-prefs", parent: body);
            EditorUI.Help("Stored in the user profile. Use it for preferences that are not tied to one project.", parent: section);

            TextField userName = new("Your name") { value = EditorPrefs.GetString(UserNameKey, string.Empty), isDelayed = true };
            userName.RegisterValueChangedCallback(OnUserNameChanged);
            section.Add(userName);
        }

        private void BuildSingletonSection(VisualElement body, VisualElement root)
        {
            Foldout section = EditorUI.Section("ScriptableSingleton: per user, this project", "pretzel-basics-singleton", parent: body);
            EditorUI.Help("Bound fields write to the singleton. The file is saved a moment after the last change.", parent: section);
            Label path = new(BasicsSettings.StoragePath);
            path.AddToClassList("pretzel-sample__caption");
            section.Add(path);

            section.Add(new Slider("Volume", 0f, 1f) { bindingPath = BasicsSettings.VolumePath, showInputField = true });
            section.Add(new Toggle("Highlight") { bindingPath = BasicsSettings.HighlightPath });
            section.Add(new TextField("Note") { bindingPath = BasicsSettings.NotePath });

            _settings = new SerializedObject(BasicsSettings.instance);
            section.Bind(_settings);
            section.TrackSerializedObjectValue(_settings, OnSettingsChanged);

            _saveTask = root.schedule.Execute(SaveSettings);
            _saveTask.Pause();
        }

        private void BuildFieldsSection(VisualElement body)
        {
            Foldout section = EditorUI.Section("Fields, buttons and help boxes", "pretzel-basics-fields", parent: body);

            _selectionLabel = new Label();
            _selectionLabel.AddToClassList("pretzel-sample__status");
            section.Add(_selectionLabel);
            OnSelectionChanged();

            _pickField = new ObjectField("Object") { objectType = typeof(UnityEngine.Object), allowSceneObjects = true };
            _pickField.RegisterValueChangedCallback(OnPickChanged);
            section.Add(_pickField);

            _pickHelp = EditorUI.Help("Pick an object to enable the buttons.", HelpBoxMessageType.Warning, section);

            section.Add(EditorUI.ButtonRow(
                ("Ping", PingPicked),
                ("Select", SelectPicked),
                ("Use Selection", UseSelection)));
        }

        private void OnTipsToggled(ChangeEvent<bool> evt)
        {
            EditorPrefs.SetBool(ShowTipsKey, evt.newValue);
            _tips.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void OnCounterChanged(ChangeEvent<int> evt)
        {
            SessionState.SetInt(CounterKey, evt.newValue);
        }

        private static void OnUserNameChanged(ChangeEvent<string> evt)
        {
            EditorPrefs.SetString(UserNameKey, evt.newValue);
        }

        // Saving on every slider tick would write the file constantly, so the save is debounced.
        private void OnSettingsChanged(SerializedObject changed)
        {
            _savePending = true;
            _saveTask.ExecuteLater(SaveDelayMilliseconds);
        }

        private void SaveSettings()
        {
            _savePending = false;
            BasicsSettings.instance.SaveSettings();
        }

        private void OnSelectionChanged()
        {
            if (_selectionLabel == null)
            {
                return;
            }

            UnityEngine.Object active = Selection.activeObject;
            _selectionLabel.text = active ? "Selection: " + active.name : "Selection: nothing";
        }

        private void OnPickChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            _pickHelp.style.display = evt.newValue ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void PingPicked()
        {
            if (_pickField.value)
            {
                EditorGUIUtility.PingObject(_pickField.value);
            }
        }

        private void SelectPicked()
        {
            if (_pickField.value)
            {
                Selection.activeObject = _pickField.value;
            }
        }

        private void UseSelection()
        {
            _pickField.value = Selection.activeObject;
        }

        private static void LogState()
        {
            BasicsSettings settings = BasicsSettings.instance;
            Debug.Log($"Counter {SessionState.GetInt(CounterKey, 0)}, name '{EditorPrefs.GetString(UserNameKey, string.Empty)}', volume {settings.Volume:0.00}, highlight {settings.Highlight}, note '{settings.Note}'.");
        }

        private void ResetState()
        {
            SessionState.EraseInt(CounterKey);
            EditorPrefs.DeleteKey(UserNameKey);
            EditorPrefs.DeleteKey(ShowTipsKey);

            _settings?.Dispose();
            _settings = null;
            rootVisualElement.Clear();
            rootVisualElement.styleSheets.Clear();
            CreateGUI();
        }
    }
}
