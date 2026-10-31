namespace Pretzel.Multiplayer
{
    /// <summary>Settings shared by every way of starting or joining a session.</summary>
    public class SessionRequest
    {
        public string DisplayName { get; set; } = "Player";

        /// <summary>Players that share this device and are owned by this client (couch co-op). Each one gets a LocalPlayerIndex.</summary>
        public int LocalPlayerCount { get; set; } = 1;

        /// <summary>When true the session is not limited by platform. When false, browsing and quick-join only show the same platform tag.</summary>
        public bool CrossPlay { get; set; } = true;

        /// <summary>Defaults to <see cref="PlatformTags.Current"/> when empty.</summary>
        public string PlatformTag { get; set; }
    }
}
