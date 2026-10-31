using System;
using System.Collections.Generic;
using Pretzel.Core;
using Pretzel.Data;

namespace Pretzel.Cloud
{
    /// <summary>
    /// The persistent upload queue and sync history of a <see cref="SyncedDataStore"/>, saved through the local store so it survives restarts.
    /// Plain data and rules only; it never touches the cloud. Public fields are the stored keys, so keep their names.
    /// </summary>
    [Serializable]
    public sealed class SyncJournal
    {
        public int version = 1;
        public long nextSequence = 1;
        public List<SyncEntry> entries = new();

        [NonSerialized]
        private Dictionary<string, SyncEntry> _index;

        public SyncEntry Find(string cloudKey)
        {
            Guard.NotNullOrEmpty(cloudKey, nameof(cloudKey));
            return Index().TryGetValue(cloudKey, out SyncEntry entry) ? entry : null;
        }

        /// <summary>Records a device save and queues the payload for upload. Replaces an earlier pending change of the same key.</summary>
        public SyncEntry RecordWrite(DataKey key, string cloudKey, long savedAtUtcTicks, string payload)
        {
            SyncEntry entry = GetOrAdd(key, cloudKey);
            entry.state = SyncEntryState.PendingWrite;
            entry.savedAtUtcTicks = savedAtUtcTicks;
            entry.payload = payload ?? string.Empty;
            entry.sequence = nextSequence++;
            ClearError(entry);
            return entry;
        }

        /// <summary>Records a device delete and queues it. Replaces an earlier pending change of the same key.</summary>
        public SyncEntry RecordDelete(DataKey key, string cloudKey, long deletedAtUtcTicks)
        {
            SyncEntry entry = GetOrAdd(key, cloudKey);
            entry.state = SyncEntryState.PendingDelete;
            entry.savedAtUtcTicks = deletedAtUtcTicks;
            entry.payload = string.Empty;
            entry.sequence = nextSequence++;
            ClearError(entry);
            return entry;
        }

        /// <summary>Records that the device and the cloud agree on a save time, creating the entry when the key was not tracked.</summary>
        public SyncEntry TrackSynced(DataKey key, string cloudKey, long savedAtUtcTicks, string writeLock)
        {
            SyncEntry entry = GetOrAdd(key, cloudKey);
            MarkSynced(entry, savedAtUtcTicks, writeLock);
            return entry;
        }

        public void MarkSynced(SyncEntry entry, long savedAtUtcTicks, string writeLock)
        {
            Guard.NotNull(entry, nameof(entry));
            entry.state = SyncEntryState.Synced;
            entry.savedAtUtcTicks = savedAtUtcTicks;
            entry.writeLock = writeLock ?? string.Empty;
            entry.payload = string.Empty;
            ClearError(entry);
        }

        /// <summary>The cloud copy is newer than the device copy; the next load downloads it.</summary>
        public void MarkCloudNewer(SyncEntry entry, long cloudSavedAtUtcTicks, string writeLock)
        {
            Guard.NotNull(entry, nameof(entry));
            entry.state = SyncEntryState.CloudNewer;
            entry.savedAtUtcTicks = cloudSavedAtUtcTicks;
            entry.writeLock = writeLock ?? string.Empty;
            entry.payload = string.Empty;
            ClearError(entry);
        }

        public void MarkFailed(SyncEntry entry, CloudError error)
        {
            Guard.NotNull(entry, nameof(entry));
            Guard.NotNull(error, nameof(error));
            entry.state = SyncEntryState.Failed;
            entry.errorKind = (int)error.Kind;
            entry.errorMessage = error.Message;
        }

        public void Remove(SyncEntry entry)
        {
            Guard.NotNull(entry, nameof(entry));
            if (entries.Remove(entry))
            {
                Index().Remove(entry.cloudKey);
            }
        }

        /// <summary>Entries with a queued upload or delete, oldest change first.</summary>
        public IReadOnlyList<SyncEntry> PendingInOrder()
        {
            List<SyncEntry> pending = new();
            foreach (SyncEntry entry in entries)
            {
                if (entry.IsPendingUpload())
                {
                    pending.Add(entry);
                }
            }

            pending.Sort((a, b) => a.sequence.CompareTo(b.sequence));
            return pending;
        }

        public int CountPending()
        {
            int count = 0;
            foreach (SyncEntry entry in entries)
            {
                if (entry.IsPendingUpload())
                {
                    count++;
                }
            }

            return count;
        }

        public int CountFailed()
        {
            int count = 0;
            foreach (SyncEntry entry in entries)
            {
                if (entry.state == SyncEntryState.Failed)
                {
                    count++;
                }
            }

            return count;
        }

        private SyncEntry GetOrAdd(DataKey key, string cloudKey)
        {
            Guard.NotNullOrEmpty(cloudKey, nameof(cloudKey));
            Dictionary<string, SyncEntry> index = Index();
            if (index.TryGetValue(cloudKey, out SyncEntry existing))
            {
                return existing;
            }

            SyncEntry entry = new()
            {
                cloudKey = cloudKey,
                name = key.Name,
                slot = key.Slot ?? -1,
                folder = key.Folder ?? string.Empty
            };
            entries.Add(entry);
            index[cloudKey] = entry;
            return entry;
        }

        private Dictionary<string, SyncEntry> Index()
        {
            if (_index == null)
            {
                _index = new Dictionary<string, SyncEntry>(StringComparer.Ordinal);
                entries ??= new List<SyncEntry>();
                foreach (SyncEntry entry in entries)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.cloudKey))
                    {
                        _index[entry.cloudKey] = entry;
                    }
                }
            }

            return _index;
        }

        private static void ClearError(SyncEntry entry)
        {
            entry.errorKind = 0;
            entry.errorMessage = string.Empty;
        }
    }
}
