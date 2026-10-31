using System.Collections.Generic;
using Pretzel.UI.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.EditorWindows
{
    /// <summary>UXML workflow: TabView and Tab declared in UXML; the window restores the active tab and adds a closeable tab from code.</summary>
    public sealed class TabsWindow : EditorWindow
    {
        private const string ActiveTabKey = "Pretzel.Samples.UI.Tabs.ActiveTab";

        private readonly List<Tab> _tabs = new();
        private TabView _tabView;
        private Label _status;
        private int _dynamicCount;

        [MenuItem("Pretzel/Samples/Editor Windows/Tabs")]
        private static void Open()
        {
            TabsWindow window = GetWindow<TabsWindow>();
            window.titleContent = new GUIContent("Tabs");
            window.minSize = new Vector2(340f, 300f);
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.AddToClassList("pretzel-sample");
            EditorUI.ApplySkinClass(root);
            SampleAssets.AddStyleSheets(root, "SampleWindows.uss", "TabsWindow.uss");
            if (!SampleAssets.CloneLayout(root, "TabsWindow.uxml"))
            {
                return;
            }

            _tabView = root.Q<TabView>("tabs__view");
            _status = root.Q<Label>("tabs__status");
            _dynamicCount = 0;

            DropdownField mode = root.Q<DropdownField>("tabs__mode");
            mode.choices = new List<string> { "Fast", "Balanced", "Quality" };
            mode.index = 1;
            root.Q<Button>("tabs__add").clicked += AddTab;

            RestoreActiveTab();
            _tabView.activeTabChanged += OnActiveTabChanged;
        }

        private void RestoreActiveTab()
        {
            _tabs.Clear();
            _tabView.Query<Tab>().ToList(_tabs);
            int saved = SessionState.GetInt(ActiveTabKey, 0);
            if (saved > 0 && saved < _tabs.Count)
            {
                _tabView.activeTab = _tabs[saved];
            }
        }

        private void OnActiveTabChanged(Tab previous, Tab next)
        {
            if (next == null)
            {
                return;
            }

            _tabs.Clear();
            _tabView.Query<Tab>().ToList(_tabs);
            SessionState.SetInt(ActiveTabKey, _tabs.IndexOf(next));
            _status.text = "Active tab: " + next.label;
        }

        private void AddTab()
        {
            _dynamicCount++;
            Tab tab = new("Dynamic " + _dynamicCount) { closeable = true };
            tab.AddToClassList("pretzel-tabs__tab");
            tab.Add(new Label("This tab was created in code. Use its close button to remove it."));
            _tabView.Add(tab);
            _tabView.activeTab = tab;
        }
    }
}
