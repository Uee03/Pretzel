namespace Pretzel.Multiplayer
{
    public sealed class SessionQuery
    {
        public bool CrossPlay { get; set; } = true;

        public string PlatformTag { get; set; }

        /// <summary>Maximum sessions to return.</summary>
        public int Count { get; set; } = 20;
    }
}
