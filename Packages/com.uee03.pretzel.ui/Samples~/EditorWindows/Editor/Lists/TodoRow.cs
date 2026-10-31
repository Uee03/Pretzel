using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.EditorWindows
{
    /// <summary>
    /// One list row. The callback is registered once in the constructor and the bound item is kept in a field,
    /// so binding a row allocates nothing and needs no per-bind closure.
    /// </summary>
    public sealed class TodoRow : VisualElement
    {
        public const string RowClass = "pretzel-lists__row";
        public const string DoneClass = "pretzel-lists__row--done";

        private readonly Toggle _done;
        private readonly Label _title;
        private TodoItem _item;

        public TodoRow()
        {
            AddToClassList(RowClass);

            _done = new Toggle();
            _done.AddToClassList("pretzel-lists__done");
            _done.RegisterValueChangedCallback(OnDoneChanged);
            Add(_done);

            _title = new Label();
            _title.AddToClassList("pretzel-lists__title");
            Add(_title);
        }

        public void Bind(TodoItem item)
        {
            _item = item;
            _done.SetValueWithoutNotify(item.Done);
            _title.text = item.Title;
            EnableInClassList(DoneClass, item.Done);
        }

        public void Unbind()
        {
            _item = null;
        }

        private void OnDoneChanged(ChangeEvent<bool> evt)
        {
            if (_item == null)
            {
                return;
            }

            _item.Done = evt.newValue;
            EnableInClassList(DoneClass, evt.newValue);
        }
    }
}
