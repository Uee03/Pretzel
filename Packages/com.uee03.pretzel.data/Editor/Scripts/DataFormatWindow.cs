using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Data.Editor
{
    /// <summary>Configures how <see cref="Pretzel.Data.DataIO"/> saves: format, encryption, key storage, shipping and a decrypt tool.</summary>
    public sealed class DataFormatWindow : EditorWindow
    {
        private const string TabKey = "Pretzel.DataFormat.Tab";

        private readonly List<IDataFormatPanel> _panels = new();
        private DataFormatState _state;
        private TabView _tabView;

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
            _panels.Add(new FormatPanel());
            _panels.Add(new EncryptionPanel());
            _panels.Add(new KeysPanel());
            _panels.Add(new ShippingPanel());
            _panels.Add(new DecryptPanel());

            _tabView = root.Q<TabView>("data-window__tabs");
            foreach (IDataFormatPanel panel in _panels)
            {
                Tab tab = new(panel.Title);
                tab.Add(panel.CreateView(_state));
                _tabView.Add(tab);
            }

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
            _state?.Flush();
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
