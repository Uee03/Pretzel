namespace Pretzel.Cloud
{
    /// <summary>Where one key stands. Stored in the journal, so values are stable.</summary>
    public enum SyncEntryState
    {
        /// <summary>The cloud held this save time when last checked.</summary>
        Synced = 0,

        /// <summary>The device copy changed and is queued for upload.</summary>
        PendingWrite = 1,

        /// <summary>The device copy was deleted and the delete is queued.</summary>
        PendingDelete = 2,

        /// <summary>The cloud copy is newer. The next load downloads it.</summary>
        CloudNewer = 3,

        /// <summary>The service rejected the upload for good, for example an invalid key. Saving the key again retries.</summary>
        Failed = 4
    }
}
