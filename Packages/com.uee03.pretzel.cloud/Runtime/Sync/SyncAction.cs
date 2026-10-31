namespace Pretzel.Cloud
{
    /// <summary>What reconciling one key's device copy with its cloud copy requires.</summary>
    public enum SyncAction
    {
        /// <summary>Both sides already agree.</summary>
        InSync = 0,

        /// <summary>Upload the device copy, or the delete when the device copy was deleted.</summary>
        PushLocal = 1,

        /// <summary>Replace the device copy with the cloud copy.</summary>
        PullCloud = 2
    }
}
