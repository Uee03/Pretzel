using Pretzel.Core;
using Pretzel.Data;

namespace Pretzel.Cloud
{
    /// <summary>
    /// The pure reconcile rules for one key, with no I/O. The <see cref="IConflictResolver"/> is asked only when the device
    /// and the cloud both changed since they last agreed; a side that merely fell behind is fast-forwarded without asking.
    /// </summary>
    public static class SyncPlanner
    {
        /// <param name="entry">The journal entry, or null when the key was never synced.</param>
        /// <param name="localExists">True when the device holds data for the key.</param>
        /// <param name="cloudExists">True when the cloud holds data for the key.</param>
        /// <param name="cloudSavedAtUtcTicks">The save time stored in the cloud payload. Ignored when <paramref name="cloudExists"/> is false.</param>
        /// <param name="wasConflict">True when the resolver decided.</param>
        public static SyncAction Plan(SyncEntry entry, bool localExists, bool cloudExists, long cloudSavedAtUtcTicks, DataKey key, IConflictResolver resolver, out bool wasConflict)
        {
            Guard.NotNull(resolver, nameof(resolver));
            wasConflict = false;
            bool localDeleted = entry != null && entry.state == SyncEntryState.PendingDelete;

            if (!cloudExists)
            {
                if (localDeleted)
                {
                    return SyncAction.InSync;
                }

                return localExists ? SyncAction.PushLocal : SyncAction.InSync;
            }

            if (localDeleted)
            {
                wasConflict = true;
                return Resolve(resolver, key, entry.savedAtUtcTicks, cloudSavedAtUtcTicks, true);
            }

            if (!localExists)
            {
                return SyncAction.PullCloud;
            }

            if (entry != null && entry.state == SyncEntryState.CloudNewer)
            {
                return SyncAction.PullCloud;
            }

            long localTicks = entry != null ? entry.savedAtUtcTicks : 0L;
            if (entry != null && cloudSavedAtUtcTicks == localTicks)
            {
                return SyncAction.InSync;
            }

            if (entry != null && entry.state == SyncEntryState.Synced && cloudSavedAtUtcTicks > localTicks)
            {
                return SyncAction.PullCloud;
            }

            wasConflict = true;
            return Resolve(resolver, key, localTicks, cloudSavedAtUtcTicks, false);
        }

        private static SyncAction Resolve(IConflictResolver resolver, DataKey key, long localTicks, long cloudTicks, bool localIsDeletion)
        {
            ConflictChoice choice = resolver.Resolve(new ConflictInfo(key, localTicks, cloudTicks, localIsDeletion));
            return choice == ConflictChoice.UseCloud ? SyncAction.PullCloud : SyncAction.PushLocal;
        }
    }
}
