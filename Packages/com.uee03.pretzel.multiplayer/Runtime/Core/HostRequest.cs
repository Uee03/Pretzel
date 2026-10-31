namespace Pretzel.Multiplayer
{
    public sealed class HostRequest : SessionRequest
    {
        public string SessionName { get; set; } = "Pretzel match";

        /// <summary>Maximum clients in the session, including the host. Ignored in local mode.</summary>
        public int MaxPlayers { get; set; } = 4;

        public bool IsPrivate { get; set; }
    }
}
