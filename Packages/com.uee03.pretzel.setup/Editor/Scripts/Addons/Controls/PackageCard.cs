using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Pretzel.Setup.Addons.Controls
{
    public enum ChipKind
    {
        Ok,
        Warn,
        Missing,
        Neutral
    }

    public readonly struct CardChip
    {
        public readonly string Text;
        public readonly ChipKind Kind;

        public CardChip(string text, ChipKind kind)
        {
            Text = text;
            Kind = kind;
        }
    }

    public readonly struct CardAction
    {
        public readonly string Label;
        public readonly Action Invoke;
        public readonly bool Enabled;
        public readonly bool Primary;
        public readonly bool Danger;
        public readonly string Tooltip;

        public CardAction(string label, Action invoke, bool enabled = true, bool primary = false, bool danger = false, string tooltip = null)
        {
            Label = label;
            Invoke = invoke;
            Enabled = enabled;
            Primary = primary;
            Danger = danger;
            Tooltip = tooltip;
        }
    }

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

    /// <summary>A card with a title, status chips, a description and action buttons. Built once from a <see cref="CardModel"/>.</summary>
    public sealed class PackageCard : VisualElement
    {
        public PackageCard(CardModel model)
        {
            AddToClassList("pretzel-card");

            VisualElement header = new();
            header.AddToClassList("pretzel-card__header");

            VisualElement titles = new();
            titles.AddToClassList("pretzel-card__titles");
            titles.Add(CreateLabel(model.Title, "pretzel-card__title"));
            if (!string.IsNullOrEmpty(model.Subtitle))
            {
                titles.Add(CreateLabel(model.Subtitle, "pretzel-card__subtitle"));
            }

            header.Add(titles);

            VisualElement chips = new();
            chips.AddToClassList("pretzel-card__chips");
            foreach (CardChip chip in model.Chips)
            {
                Label label = CreateLabel(chip.Text, "pretzel-chip");
                label.AddToClassList(chip.Kind switch
                {
                    ChipKind.Ok => "pretzel-chip--ok",
                    ChipKind.Warn => "pretzel-chip--warn",
                    ChipKind.Missing => "pretzel-chip--missing",
                    _ => "pretzel-chip--neutral"
                });
                chips.Add(label);
            }

            header.Add(chips);
            Add(header);

            if (!string.IsNullOrEmpty(model.Description))
            {
                Add(CreateLabel(model.Description, "pretzel-card__description"));
            }

            if (!string.IsNullOrEmpty(model.Detail))
            {
                Add(CreateLabel(model.Detail, "pretzel-card__detail"));
            }

            foreach (VisualElement extra in model.Extras)
            {
                Add(extra);
            }

            if (!string.IsNullOrEmpty(model.Note))
            {
                HelpBox note = new(model.Note, model.NoteType);
                note.AddToClassList("pretzel-card__note");
                Add(note);
            }

            if (model.Actions.Count > 0)
            {
                VisualElement actions = new();
                actions.AddToClassList("pretzel-card__actions");
                foreach (CardAction action in model.Actions)
                {
                    Button button = new(action.Invoke) { text = action.Label, tooltip = action.Tooltip };
                    button.AddToClassList("pretzel-card__action");
                    button.EnableInClassList("pretzel-card__action--primary", action.Primary);
                    button.EnableInClassList("pretzel-card__action--danger", action.Danger);
                    button.SetEnabled(action.Enabled);
                    actions.Add(button);
                }

                Add(actions);
            }
        }

        private static Label CreateLabel(string text, string className)
        {
            Label label = new(text);
            label.AddToClassList(className);
            return label;
        }
    }
}
