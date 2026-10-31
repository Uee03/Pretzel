using Pretzel.Data;

namespace Pretzel.Cloud
{
    /// <summary>The two competing versions of one key, passed to an <see cref="IConflictResolver"/>.</summary>
    public sealed class ConflictInfo
    {
        public DataKey Key { get; }

        /// <summary>When the device copy was last saved, in UTC ticks. 0 when unknown, for example data saved before syncing was added.</summary>
        public long LocalSavedAtUtcTicks { get; }

        /// <summary>When the cloud copy was saved by a device, in UTC ticks.</summary>
        public long CloudSavedAtUtcTicks { get; }

        /// <summary>True when the device copy is a pending delete rather than data.</summary>
        public bool LocalIsDeletion { get; }

        public bool LocalIsNewer => LocalSavedAtUtcTicks > CloudSavedAtUtcTicks;

        public bool CloudIsNewer => CloudSavedAtUtcTicks > LocalSavedAtUtcTicks;

        public ConflictInfo(DataKey key, long localSavedAtUtcTicks, long cloudSavedAtUtcTicks, bool localIsDeletion)
        {
            Key = key;
            LocalSavedAtUtcTicks = localSavedAtUtcTicks;
            CloudSavedAtUtcTicks = cloudSavedAtUtcTicks;
            LocalIsDeletion = localIsDeletion;
        }
    }
}
