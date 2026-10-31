using System;
using System.Collections.Generic;
using System.IO;
using Pretzel.UI.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.EditorWindows
{
    /// <summary>
    /// A MultiColumnListView over the project's assets: sortable and stretchable columns, a data model class,
    /// filtering with a ToolbarSearchField and a button column. Cells are created once and rebound, never rebuilt.
    /// </summary>
    public sealed class TablesWindow : EditorWindow
    {
        private const int MaxRows = 2000;
        private const string NameColumn = "name";
        private const string TypeColumn = "type";
        private const string SizeColumn = "size";

        private readonly List<AssetRow> _all = new();
        private readonly List<AssetRow> _visible = new();
        private MultiColumnListView _table;
        private ToolbarSearchField _search;
        private Label _status;
        private string _filter = string.Empty;
        private string _sortColumn = NameColumn;
        private bool _sortDescending;

        [MenuItem("Pretzel/Samples/Editor Windows/Tables")]
        private static void Open()
        {
            TablesWindow window = GetWindow<TablesWindow>();
            window.titleContent = new GUIContent("Tables");
            window.minSize = new Vector2(480f, 320f);
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.AddToClassList("pretzel-sample");
            EditorUI.ApplySkinClass(root);
            SampleAssets.AddStyleSheets(root, "SampleWindows.uss", "TablesWindow.uss");

            Toolbar toolbar = new();
            toolbar.Add(new ToolbarButton(Reload) { text = "Refresh" });
            toolbar.Add(new ToolbarSpacer { flex = true });
            _search = new ToolbarSearchField();
            _search.AddToClassList("pretzel-tables__search");
            _search.RegisterValueChangedCallback(OnSearchChanged);
            toolbar.Add(_search);
            root.Add(toolbar);

            _table = new MultiColumnListView
            {
                name = "tables__table",
                fixedItemHeight = 22f,
                showBorder = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                selectionType = SelectionType.Single,
                // Custom: this window sorts its own list when the user clicks a header.
                sortingMode = ColumnSortingMode.Custom,
                itemsSource = _visible,
            };
            _table.AddToClassList("pretzel-tables__table");

            _table.columns.Add(new Column { name = NameColumn, title = "Name", width = 200f, stretchable = true, sortable = true, makeCell = MakeTextCell, bindCell = BindNameCell });
            _table.columns.Add(new Column { name = TypeColumn, title = "Type", width = 110f, sortable = true, makeCell = MakeMutedCell, bindCell = BindTypeCell });
            _table.columns.Add(new Column { name = SizeColumn, title = "Size (KB)", width = 90f, sortable = true, makeCell = MakeNumberCell, bindCell = BindSizeCell });
            _table.columns.Add(new Column { name = "ping", title = string.Empty, width = 60f, sortable = false, makeCell = MakePingCell, bindCell = BindPingCell, unbindCell = UnbindPingCell });

            _table.columnSortingChanged += OnSortingChanged;
            _table.itemsChosen += OnItemsChosen;
            root.Add(_table);

            _status = new Label();
            _status.AddToClassList("pretzel-sample__status");
            root.Add(_status);

            Reload();
        }

        private static VisualElement MakeTextCell()
        {
            Label label = new();
            label.AddToClassList("pretzel-tables__cell");
            return label;
        }

        private static VisualElement MakeMutedCell()
        {
            Label label = new();
            label.AddToClassList("pretzel-tables__cell");
            label.AddToClassList("pretzel-tables__cell--muted");
            return label;
        }

        private static VisualElement MakeNumberCell()
        {
            Label label = new();
            label.AddToClassList("pretzel-tables__cell");
            label.AddToClassList("pretzel-tables__cell--number");
            return label;
        }

        private static VisualElement MakePingCell()
        {
            return new AssetPingButton();
        }

        private void BindNameCell(VisualElement cell, int index)
        {
            ((Label)cell).text = _visible[index].Name;
            cell.tooltip = _visible[index].Path;
        }

        private void BindTypeCell(VisualElement cell, int index)
        {
            ((Label)cell).text = _visible[index].TypeName;
        }

        private void BindSizeCell(VisualElement cell, int index)
        {
            ((Label)cell).text = (_visible[index].SizeBytes / 1024f).ToString("0.0");
        }

        private void BindPingCell(VisualElement cell, int index)
        {
            ((AssetPingButton)cell).Row = _visible[index];
        }

        private static void UnbindPingCell(VisualElement cell, int index)
        {
            ((AssetPingButton)cell).Row = null;
        }

        private void Reload()
        {
            _all.Clear();
            foreach (string path in AssetDatabase.GetAllAssetPaths())
            {
                if (_all.Count >= MaxRows)
                {
                    break;
                }

                if (!path.StartsWith("Assets/", StringComparison.Ordinal))
                {
                    continue;
                }

                FileInfo info = new(path);
                if (!info.Exists)
                {
                    continue;
                }

                Type type = AssetDatabase.GetMainAssetTypeAtPath(path);
                _all.Add(new AssetRow(path, Path.GetFileName(path), type != null ? type.Name : "Unknown", info.Length));
            }

            ApplyFilter();
        }

        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            _filter = evt.newValue ?? string.Empty;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            _visible.Clear();
            bool all = _filter.Length == 0;
            for (int i = 0; i < _all.Count; i++)
            {
                AssetRow row = _all[i];
                if (all || row.Path.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                {
                    _visible.Add(row);
                }
            }

            _visible.Sort(CompareRows);
            _table.RefreshItems();
            _status.text = $"{_visible.Count} of {_all.Count} assets (first {MaxRows} in Assets). Click a header to sort, double-click a row to open it.";
        }

        private void OnSortingChanged()
        {
            foreach (SortColumnDescription description in _table.sortedColumns)
            {
                _sortColumn = description.columnName;
                _sortDescending = description.direction == SortDirection.Descending;
                break;
            }

            _visible.Sort(CompareRows);
            _table.RefreshItems();
        }

        private int CompareRows(AssetRow a, AssetRow b)
        {
            int result;
            if (string.Equals(_sortColumn, SizeColumn, StringComparison.Ordinal))
            {
                result = a.SizeBytes.CompareTo(b.SizeBytes);
            }
            else if (string.Equals(_sortColumn, TypeColumn, StringComparison.Ordinal))
            {
                result = string.Compare(a.TypeName, b.TypeName, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                result = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            }

            return _sortDescending ? -result : result;
        }

        private static void OnItemsChosen(IEnumerable<object> chosen)
        {
            foreach (object item in chosen)
            {
                UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(((AssetRow)item).Path);
                if (asset)
                {
                    AssetDatabase.OpenAsset(asset);
                }
            }
        }
    }
}
