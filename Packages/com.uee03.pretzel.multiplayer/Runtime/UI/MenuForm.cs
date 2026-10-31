namespace Pretzel.Multiplayer.UI
{
    /// <summary>The values the player typed or toggled. Read from the view when an action needs them.</summary>
    public sealed class MenuForm
    {
        public string DisplayName { get; set; } = "Player";

        public bool CrossPlay { get; set; } = true;

        public int LocalPlayers { get; set; } = 1;

        public string SessionName { get; set; } = "Pretzel match";

        public int MaxPlayers { get; set; } = 4;

        public bool IsPrivate { get; set; }

        public string JoinCode { get; set; } = string.Empty;
    }
}
