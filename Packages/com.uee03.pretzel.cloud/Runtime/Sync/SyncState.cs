namespace Pretzel.Cloud
{
    public enum SyncState
    {
        /// <summary>Nothing is waiting for upload and the last cloud call succeeded.</summary>
        Synced = 0,

        /// <summary>Changes are saved on the device and queued for upload.</summary>
        Pending = 1,

        /// <summary>A cloud call is running.</summary>
        Syncing = 2,

        /// <summary>The last cloud call could not reach the service. Queued changes upload when it can.</summary>
        Offline = 3,

        /// <summary>The player is not signed in, so nothing can sync yet.</summary>
        NotSignedIn = 4,

        /// <summary>The last cloud call failed for another reason, or the service rejected a key for good.</summary>
        Error = 5
    }
}
