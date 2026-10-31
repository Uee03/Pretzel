namespace Pretzel.Multiplayer
{
    public enum SessionMode
    {
        /// <summary>Same-machine couch co-op: a NetworkManager host on loopback, no cloud, no authentication.</summary>
        Local = 0,

        /// <summary>A Unity Multiplayer Services session with Relay.</summary>
        Online = 1,
    }
}
