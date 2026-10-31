using System;
using Pretzel.Data;

namespace Pretzel.Cloud
{
    /// <summary>
    /// Sync bookkeeping for one key. Public fields are the stored keys of the journal file, so keep their names.
    /// The queued payload is held here while an upload is pending, which suits small player data.
    /// </summary>
    [Serializable]
    public sealed class SyncEntry
    {
        public string cloudKey = string.Empty;
        public string name = string.Empty;

        /// <summary>The save slot, or -1 when the key is not slotted.</summary>
        public int slot = -1;

        public string folder = string.Empty;
        public SyncEntryState state;

        /// <summary>When the device copy was last saved or deleted, or the cloud copy's save time once synced.</summary>
        public long savedAtUtcTicks;

        /// <summary>The cloud write lock last seen for this key.</summary>
        public string writeLock = string.Empty;

        /// <summary>Order of the latest change, used to upload in the order the player made them.</summary>
        public long sequence;

        /// <summary>The cloud payload to upload while <see cref="state"/> is <see cref="SyncEntryState.PendingWrite"/>.</summary>
        public string payload = string.Empty;

        public int errorKind;
        public string errorMessage = string.Empty;

        public DataKey ToDataKey() => new(name, slot < 0 ? null : slot, string.IsNullOrEmpty(folder) ? null : folder);

        // A method, not a property, so the JSON serializer does not write it into the journal.
        public bool IsPendingUpload() => state == SyncEntryState.PendingWrite || state == SyncEntryState.PendingDelete;
    }
}
