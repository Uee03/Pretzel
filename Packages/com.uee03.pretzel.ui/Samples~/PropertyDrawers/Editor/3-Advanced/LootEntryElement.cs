using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.PropertyDrawers.Editors
{
    /// <summary>
    /// The visual tree for one <see cref="LootEntry"/>. It is an element, not drawer code, because it keeps state (the choices,
    /// the badge, the properties it tracks) and subscribes to the catalog only while it is on a panel.
    /// </summary>
    public sealed class LootEntryElement : VisualElement
    {
        private readonly List<string> _choices = new();
        private readonly SerializedProperty _itemId;
        private readonly SerializedProperty _minCount;
        private readonly SerializedProperty _maxCount;
        private readonly SerializedProperty _weight;
        private readonly DropdownField _item;
        private readonly StatusBadge _badge;
        private readonly HelpBox _help;
        private readonly Button _createCatalog;

        public LootEntryElement(SerializedProperty property)
        {
            AddToClassList("pretzel-drawer");
            AddToClassList("pretzel-drawer__box");
            SampleAssets.AddStyleSheet(this, "PropertyDrawerSamples.uss");

            _itemId = property.FindPropertyRelative(nameof(LootEntry.itemId));
            _minCount = property.FindPropertyRelative(nameof(LootEntry.minCount));
            _maxCount = property.FindPropertyRelative(nameof(LootEntry.maxCount));
            _weight = property.FindPropertyRelative(nameof(LootEntry.weight));

            // The dropdown's choices come from the data source; the selected value is bound to the string property.
            _item = new DropdownField("Item");
            _item.AddToClassList("pretzel-drawer__cell");
            _item.BindProperty(_itemId);
            _badge = new StatusBadge();

            VisualElement header = new();
            header.AddToClassList("pretzel-drawer__row");
            header.Add(_item);
            header.Add(_badge);
            Add(header);

            VisualElement counts = new();
            counts.AddToClassList("pretzel-drawer__row");
            counts.Add(Cell(_minCount, "Min"));
            counts.Add(Cell(_maxCount, "Max"));
            PropertyField weightField = Cell(_weight, "Weight");
            weightField.RegisterValueChangeCallback(OnWeightChanged);
            counts.Add(weightField);
            Add(counts);

            _help = new HelpBox(string.Empty, HelpBoxMessageType.Warning);
            _help.AddToClassList("pretzel-drawer__help");
            Add(_help);

            _createCatalog = new Button(OnCreateCatalog) { text = "Create Item Catalog" };
            _createCatalog.AddToClassList("pretzel-drawer__help");
            Add(_createCatalog);

            // Reacts to the properties that feed the validation, including changes from Undo and other fields.
            this.TrackPropertyValue(_itemId, OnPropertyChanged);
            this.TrackPropertyValue(_minCount, OnPropertyChanged);
            this.TrackPropertyValue(_maxCount, OnPropertyChanged);

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);

            RefreshChoices();
            Validate();
        }

        private static PropertyField Cell(SerializedProperty property, string label)
        {
            PropertyField field = new(property, label);
            field.AddToClassList("pretzel-drawer__cell");
            return field;
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            ItemCatalogSource.Changed += OnCatalogChanged;
            OnCatalogChanged();
        }

        private void OnDetach(DetachFromPanelEvent evt)
        {
            ItemCatalogSource.Changed -= OnCatalogChanged;
        }

        private void OnCatalogChanged()
        {
            RefreshChoices();
            Validate();
        }

        private void OnPropertyChanged(SerializedProperty changed)
        {
            Validate();
        }

        private void OnWeightChanged(SerializedPropertyChangeEvent evt)
        {
            Validate();
        }

        private static void OnCreateCatalog()
        {
            ItemCatalogSource.CreateDefaultCatalog();
        }

        private void RefreshChoices()
        {
            ItemCatalogSource.FillIds(_choices);
            _item.choices = _choices;
        }

        private void Validate()
        {
            string id = _itemId.stringValue;
            bool hasCatalog = ItemCatalogSource.HasCatalog;
            _createCatalog.style.display = hasCatalog ? DisplayStyle.None : DisplayStyle.Flex;

            if (string.IsNullOrEmpty(id))
            {
                Show(BadgeStatus.Error, "Pick an item.");
            }
            else if (_minCount.intValue > _maxCount.intValue)
            {
                Show(BadgeStatus.Error, "Min is above Max.");
            }
            else if (!hasCatalog)
            {
                Show(BadgeStatus.Warning, "No Item Catalog asset found, so the id cannot be checked.");
            }
            else if (!ItemCatalogSource.Contains(id))
            {
                Show(BadgeStatus.Warning, $"'{id}' is not in the catalog.");
            }
            else if (_weight.floatValue <= 0f)
            {
                Show(BadgeStatus.Warning, "A weight of 0 never drops.");
            }
            else
            {
                Show(BadgeStatus.Ok, null);
            }
        }

        private void Show(BadgeStatus status, string message)
        {
            _badge.Status = status;
            bool any = message != null;
            _help.style.display = any ? DisplayStyle.Flex : DisplayStyle.None;
            if (any)
            {
                _help.messageType = status == BadgeStatus.Error ? HelpBoxMessageType.Error : HelpBoxMessageType.Warning;
                _help.text = message;
            }
        }
    }
}
