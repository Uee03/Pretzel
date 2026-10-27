using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Data.Editor
{
    /// <summary>Configures how <see cref="Pretzel.Data.DataIO"/> saves: setup code, format, encryption, key storage, shipping and a decrypt tool.</summary>
    public sealed class DataFormatWindow : EditorWindow
    {
        private const string TabKey = "Pretzel.DataFormat.Tab";

        private readonly List<IDataFormatPanel> _panels = new();
        private readonly List<VisualElement> _settingsViews = new();
        private DataFormatState _state;
        private TabView _tabView;
        private HelpBox _playModeNote;

        [MenuItem("Pretzel/Data Format", priority = 101)]
        public static void Open()
        {
            DataFormatWindow window = GetWindow<DataFormatWindow>();
            window.minSize = new Vector2(520f, 560f);
            window.Show();
        }

        private void CreateGUI()
        {
            Texture2D logo = DataEditorAssets.Branding("pretzel-sunflower-tile");
            titleContent = new GUIContent("Data Format", logo);

            VisualElement root = rootVisualElement;
            root.styleSheets.Add(DataEditorAssets.Style("DataFormatWindow"));
            DataEditorAssets.Layout("DataFormatWindow").CloneTree(root);
            root.Q<Image>("data-window__logo").image = logo;

            _state = new DataFormatState();
            _state.Initialize(root);
            _state.Changed += UpdateUnsaved;
            UpdateUnsaved();

            _panels.Clear();
            _panels.Add(new SetupPanel());
            _panels.Add(new FormatPanel());
            _panels.Add(new EncryptionPanel());
            _panels.Add(new KeysPanel());
            _panels.Add(new ShippingPanel());
            _panels.Add(new DecryptPanel());

            _tabView = root.Q<TabView>("data-window__tabs");
            _settingsViews.Clear();
            foreach (IDataFormatPanel panel in _panels)
            {
                Tab tab = new(panel.Title);
                VisualElement view = panel.CreateView(_state);
                tab.Add(view);
                _tabView.Add(tab);
                if (panel is not DecryptPanel)
                {
                    _settingsViews.Add(view);
                }
            }

            _playModeNote = new HelpBox("Settings are read once when the game starts and stay fixed while it runs. Stop Play mode to change them.",
                HelpBoxMessageType.Info);
            _tabView.parent.Insert(_tabView.parent.IndexOf(_tabView), _playModeNote);
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            UpdatePlayModeLock();

            _tabView.selectedTabIndex = Mathf.Clamp(SessionState.GetInt(TabKey, 0), 0, _panels.Count - 1);
            _tabView.activeTabChanged += (_, _) =>
            {
                int index = _tabView.selectedTabIndex;
                SessionState.SetInt(TabKey, index);
                if (index >= 0 && index < _panels.Count)
                {
                    _panels[index].OnSelected();
                }
            };
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            _state?.Flush();
        }

        private void OnPlayModeChanged(PlayModeStateChange _) => UpdatePlayModeLock();

        // The decrypt tool stays usable; everything that changes settings or keys is locked.
        private void UpdatePlayModeLock()
        {
            bool playing = EditorApplication.isPlayingOrWillChangePlaymode;
            foreach (VisualElement view in _settingsViews)
            {
                view.SetEnabled(!playing);
            }

            _playModeNote.style.display = playing ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void UpdateUnsaved()
        {
            hasUnsavedChanges = _state.KeysUnstored;
            saveChangesMessage = "The generated encryption keys are only in memory. Save them to this project's key file? Files encrypted with them cannot be opened if the keys are lost.";
        }

        public override void SaveChanges()
        {
            _state.SaveKeysToFile();
            base.SaveChanges();
        }

        public override void DiscardChanges()
        {
            _state.DiscardUnstoredKeys();
            base.DiscardChanges();
        }
    }
}
