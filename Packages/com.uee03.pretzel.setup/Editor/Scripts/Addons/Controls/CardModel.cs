using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Pretzel.Setup.Addons.Controls
{
    public sealed class CardModel
    {
        private readonly List<CardChip> _chips = new();
        private readonly List<CardAction> _actions = new();
        private readonly List<VisualElement> _extras = new();

        public string Title { get; set; }

        public string Subtitle { get; set; }

        public string Description { get; set; }

        public string Detail { get; set; }

        public string Note { get; set; }

        public HelpBoxMessageType NoteType { get; set; } = HelpBoxMessageType.Info;

        public IReadOnlyList<CardChip> Chips => _chips;

        public IReadOnlyList<CardAction> Actions => _actions;

        /// <summary>Extra elements shown under the description, for example a version field.</summary>
        public IReadOnlyList<VisualElement> Extras => _extras;

        public void AddChip(CardChip chip) => _chips.Add(chip);

        public void AddAction(CardAction action) => _actions.Add(action);

        public void AddExtra(VisualElement extra) => _extras.Add(extra);
    }
}
