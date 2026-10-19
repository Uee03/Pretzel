using Pretzel.Setup.Tabs;
using Pretzel.Setup.Addons.Catalog;
using Pretzel.Setup.Addons.Services;
using UnityEditor;
using UnityEngine.UIElements;

namespace Pretzel.Setup.Addons.Tabs
{
    /// <summary>Shared shell of the Addons tabs: toolbar, status line, scrolling card list, and refresh on package changes.</summary>
    public abstract class PackagesTabBase : IPretzelTab
    {
        private VisualElement _status;
        private VisualElement _list;

        protected PretzelTabContext Context { get; private set; }

        public abstract string Title { get; }

        public abstract int Order { get; }

        public VisualElement CreateView(PretzelTabContext context)
        {
            Context = context;

            VisualElement root = new();
            root.AddToClassList("pretzel-packages");
            root.style.flexGrow = 1f;
            root.styleSheets.Add(SetupAssets.Style("Addons"));

            VisualElement toolbar = new();
            toolbar.AddToClassList("pretzel-packages__toolbar");
            BuildToolbar(toolbar);
            root.Add(toolbar);

            _status = new VisualElement();
            root.Add(_status);

            ScrollView scroll = new();
            scroll.AddToClassList("pretzel-tab");
            _list = scroll;
            root.Add(scroll);

            // The services are static, so listen only while the tab is on screen.
            root.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                PackageService.Changed += Rebuild;
                CatalogService.Changed += Rebuild;
            });
            root.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                PackageService.Changed -= Rebuild;
                CatalogService.Changed -= Rebuild;
            });

            Rebuild();
            return root;
        }

        public void OnSelected()
        {
            PackageService.Refresh();
            Rebuild();
        }

        protected virtual void BuildToolbar(VisualElement toolbar)
        {
        }

        protected abstract void BuildCards(VisualElement list);

        protected void Rebuild()
        {
            if (_list == null)
            {
                return;
            }

            _status.Clear();
            if (PackageService.IsBusy)
            {
                _status.Add(new HelpBox($"{PackageService.BusyLabel}. The Editor may recompile when this finishes.", HelpBoxMessageType.Info));
            }
            else if (!string.IsNullOrEmpty(PackageService.LastError))
            {
                _status.Add(new HelpBox(PackageService.LastError, HelpBoxMessageType.Error));
            }

            ((ScrollView)_list).contentContainer.Clear();
            BuildCards(((ScrollView)_list).contentContainer);
        }
    }
}
