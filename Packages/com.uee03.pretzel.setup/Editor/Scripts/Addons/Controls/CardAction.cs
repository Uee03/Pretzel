using System;

namespace Pretzel.Setup.Addons.Controls
{
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
}
