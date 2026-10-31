using System.Collections.Generic;
using Pretzel.UI.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.EditorWindows
{
    /// <summary>
    /// A virtualized ListView over a plain C# list: makeItem/bindItem/unbindItem/destroyItem, a custom add,
    /// reordering, multi-selection and a counter that shows how few rows are created for many items.
    /// </summary>
    public sealed class ListsWindow : EditorWindow
    {
        private const float RowHeight = 22f;
        private const int InitialCount = 25;
        private const int BulkCount = 5000;

        private readonly List<TodoItem> _items = new();
        private ListView _list;
        private Label _stats;
        private int _rowsAlive;
        private int _selectedCount;

        [MenuItem("Pretzel/Samples/Editor Windows/Lists")]
        private static void Open()
        {
            ListsWindow window = GetWindow<ListsWindow>();
            window.titleContent = new GUIContent("Lists");
            window.minSize = new Vector2(320f, 380f);
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.AddToClassList("pretzel-sample");
            EditorUI.ApplySkinClass(root);
            SampleAssets.AddStyleSheets(root, "SampleWindows.uss", "ListsWindow.uss");

            Toolbar toolbar = new();
            toolbar.Add(new ToolbarButton(AddMany) { text = "Add 5,000" });
            toolbar.Add(new ToolbarButton(ClearDone) { text = "Clear Done" });
            toolbar.Add(new ToolbarButton(ClearAll) { text = "Clear All" });
            root.Add(toolbar);

            if (_items.Count == 0)
            {
                for (int i = 1; i <= InitialCount; i++)
                {
                    _items.Add(new TodoItem { Title = "Task " + i });
                }
            }

            _rowsAlive = 0;
            _selectedCount = 0;

            // A fixed row height lets the ListView virtualize cheaply.
            _list = new ListView(_items, RowHeight, MakeRow, BindRow)
            {
                name = "lists__list",
                unbindItem = UnbindRow,
                destroyItem = DestroyRow,
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                showAddRemoveFooter = true,
                showBorder = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                selectionType = SelectionType.Multiple,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
            };
            _list.AddToClassList("pretzel-lists__list");

            // The default add appends default(T), which is null for a class.
            _list.onAdd = OnAdd;
            _list.selectionChanged += OnSelectionChanged;
            _list.itemsChosen += OnItemsChosen;
            root.Add(_list);

            _stats = new Label();
            _stats.AddToClassList("pretzel-sample__status");
            root.Add(_stats);
            UpdateStats();
        }

        private VisualElement MakeRow()
        {
            _rowsAlive++;
            UpdateStats();
            return new TodoRow();
        }

        private void BindRow(VisualElement element, int index)
        {
            ((TodoRow)element).Bind(_items[index]);
        }

        private static void UnbindRow(VisualElement element, int index)
        {
            ((TodoRow)element).Unbind();
        }

        private void DestroyRow(VisualElement element)
        {
            _rowsAlive--;
            UpdateStats();
        }

        private void OnAdd(BaseListView view)
        {
            _items.Add(new TodoItem { Title = "Task " + (_items.Count + 1) });
            view.RefreshItems();
            view.ScrollToItem(-1);
            view.SetSelection(_items.Count - 1);
            UpdateStats();
        }

        private void OnSelectionChanged(IEnumerable<object> selection)
        {
            _selectedCount = 0;
            foreach (object item in selection)
            {
                _selectedCount++;
            }

            UpdateStats();
        }

        // Double-click or Enter.
        private void OnItemsChosen(IEnumerable<object> chosen)
        {
            foreach (object item in chosen)
            {
                TodoItem todo = (TodoItem)item;
                todo.Done = !todo.Done;
            }

            _list.RefreshItems();
        }

        private void AddMany()
        {
            int start = _items.Count;
            for (int i = 1; i <= BulkCount; i++)
            {
                _items.Add(new TodoItem { Title = "Task " + (start + i) });
            }

            _list.RefreshItems();
            UpdateStats();
        }

        private void ClearDone()
        {
            _items.RemoveAll(IsDone);
            _list.ClearSelection();
            _list.RefreshItems();
            UpdateStats();
        }

        private void ClearAll()
        {
            _items.Clear();
            _list.ClearSelection();
            _list.RefreshItems();
            UpdateStats();
        }

        private static bool IsDone(TodoItem item)
        {
            return item.Done;
        }

        private void UpdateStats()
        {
            if (_stats != null)
            {
                _stats.text = $"Items {_items.Count}   Rows created {_rowsAlive}   Selected {_selectedCount}";
            }
        }
    }
}
