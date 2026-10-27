using System;
using System.Collections.Generic;
using Pretzel.Setup.Settings;
using Pretzel.Setup.Tabs;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Setup.Windows
{
    public sealed class PretzelSetupWindow : EditorWindow
    {
        [MenuItem("Pretzel/Project Setup #&p", priority = 100)]
        public static void Open()
        {
            PretzelSetupWindow window = GetWindow<PretzelSetupWindow>();
            window.minSize = new Vector2(480f, 560f);
            window.Show();
        }

        private SettingsSaver _saver;
        private PretzelTabContext _context;
        private TabView _tabView;
        private SetupProgressFooter _progress;
        private readonly List<IPretzelTab> _tabs = new();
        private readonly Dictionary<Tab, IPretzelTab> _tabByElement = new();

        private void CreateGUI()
        {
            PretzelSettings.Reload();

            Texture2D logo = SetupAssets.Branding("pretzel");
            titleContent = new GUIContent("Project Setup", logo);

            VisualElement root = rootVisualElement;
            root.styleSheets.Add(SetupAssets.Style("Variables"));
            root.styleSheets.Add(SetupAssets.Style("PretzelSetupWindow"));
            SetupAssets.Layout("PretzelSetupWindow").CloneTree(root);

            root.Q<Image>("setup-window__logo").image = SetupAssets.Branding("pretzel-sunflower-tile");
            root.Q<Label>("setup-window__version").text = "Version " + SetupAssets.PackageVersion();

            _saver = new SettingsSaver(root);
            _context = new PretzelTabContext(root, _saver);

            _tabView = root.Q<TabView>("setup-window__tabs");
            _progress = new SetupProgressFooter(root);
            BuildTabs();
        }

        private void OnDisable()
        {
            _saver?.Flush();
        }

        private void BuildTabs()
        {
            _tabs.Clear();
            _tabByElement.Clear();

            foreach (Type type in TypeCache.GetTypesDerivedFrom<IPretzelTab>())
            {
                if (type.IsAbstract || type.IsInterface || type.GetConstructor(Type.EmptyTypes) == null)
                {
                    continue;
                }

                _tabs.Add((IPretzelTab)Activator.CreateInstance(type));
            }

            _tabs.Sort((a, b) =>
            {
                int byOrder = a.Order.CompareTo(b.Order);
                return byOrder != 0 ? byOrder : string.CompareOrdinal(a.Title, b.Title);
            });

            foreach (IPretzelTab tab in _tabs)
            {
                Tab element = new(tab.Title);
                element.Add(tab.CreateView(_context));
                _tabView.Add(element);
                _tabByElement[element] = tab;
            }

            int last = Mathf.Clamp(PretzelSettings.User.lastTab, 0, Mathf.Max(0, _tabs.Count - 1));
            if (_tabView.childCount > 0)
            {
                _tabView.selectedTabIndex = last;
                _tabs[last].OnSelected();
            }

            _tabView.activeTabChanged += OnActiveTabChanged;
        }

        private void OnActiveTabChanged(Tab previous, Tab next)
        {
            if (next == null || !_tabByElement.TryGetValue(next, out IPretzelTab tab))
            {
                return;
            }

            PretzelSettings.User.lastTab = _tabs.IndexOf(tab);
            _context.MarkUserDirty();
            tab.OnSelected();
        }
    }
}
