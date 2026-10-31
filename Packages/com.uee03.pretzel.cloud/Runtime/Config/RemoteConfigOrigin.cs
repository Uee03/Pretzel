namespace Pretzel.Cloud
{
    /// <summary>Where the current Remote Config values came from.</summary>
    public enum RemoteConfigOrigin
    {
        /// <summary>No fetch has happened or it failed with nothing cached: only caller defaults apply.</summary>
        Default = 0,

        /// <summary>The last fetch failed and the values come from the on-device cache.</summary>
        Cached = 1,

        /// <summary>The values were just fetched from the service.</summary>
        Remote = 2
    }
}
