namespace Pretzel.Setup.Addons.Controls
{
    public sealed class CardChip
    {
        public readonly string Text;
        public readonly ChipKind Kind;

        public CardChip(string text, ChipKind kind)
        {
            Text = text;
            Kind = kind;
        }
    }
}
