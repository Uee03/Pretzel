using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Pretzel.Setup.Addons.Controls
{
    public sealed class CardModel
    {
        public string Title;
        public string Subtitle;
        public string Description;
        public string Detail;
        public string Note;
        public HelpBoxMessageType NoteType = HelpBoxMessageType.Info;
        public List<CardChip> Chips = new();
        public List<CardAction> Actions = new();

        /// <summary>Extra elements shown under the description, for example a version field.</summary>
        public List<VisualElement> Extras = new();
    }
}
