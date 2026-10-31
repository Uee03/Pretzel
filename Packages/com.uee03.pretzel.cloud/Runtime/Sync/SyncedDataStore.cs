using System;
using System.Collections.Generic;
using System.Threading;
using Pretzel.Core;
using Pretzel.Data;
using UnityEngine;

namespace Pretzel.Cloud
{
    /// <summary>
    /// An offline-first <see cref="IDataStore"/>: every save goes to the local store first, then is queued for the cloud and uploaded
    /// with retries. Loads reconcile the device copy with the cloud copy and apply an <see cref="IConflictResolver"/> only when both changed.
    /// <para>
    /// The sync methods (<c>Save</c>, <c>Load</c>, <c>Exists</c>, <c>Delete</c>, <c>List</c>) never wait for the network: they use the local store
    /// and queue changes. The async methods also talk to the cloud, but a cloud failure never fails them; it shows in <see cref="Status"/> and the change stays queued.
    /// Use from the main thread. State is per instance and the queue is saved through the local store, so it survives restarts.
    /// </para>
    /// </summary>
    public sealed partial class SyncedDataStore : IDataStore
    {
        private const string JournalName = "sync-journal";

        private readonly IDataStore _local;
        private readonly ICloudSaveService _cloud;
        private readonly IConflictResolver _resolver;
        private readonly CloudRetry _flushRetry;
        private readonly CloudRetry _foregroundRetry;
        private readonly IRetryDelay _delay;
        private readonly Func<DateTime> _clock;
        private readonly Func<DataKey, bool> _filter;
        private readonly bool _uploadOnSave;
        private readonly DataKey _journalKey;
        private readonly Dictionary<Type, IMigration[]> _migrations = new();
        private readonly SemaphoreSlim _gate = new(1, 1);

        private SyncJournal _journal;
        private SyncStatus _status;
        private CloudError _lastError;
        private DateTime? _lastSyncedUtc;
        private int _resolvedConflicts;
        private int _syncingDepth;

        public ISerializer Serializer => _local.Serializer;

        /// <summary>Where syncing stands now. Never null.</summary>
        public SyncStatus Status
        {
            get
            {
                _status ??= BuildStatus();
                return _status;
            }
        }

        /// <summary>Keys with an upload or delete waiting.</summary>
        public int PendingCount => Journal.CountPending();

        /// <summary>Raised on the main thread when <see cref="Status"/> changes.</summary>
        public event Action<SyncStatus> StatusChanged;

        /// <param name="local">The store that always holds the data, for example <see cref="DataIO.Default"/>.</param>
        /// <param name="cloud">The cloud player data. It must be signed in for uploads to succeed; until then changes stay queued.</param>
        public SyncedDataStore(IDataStore local, ICloudSaveService cloud, SyncedDataStoreOptions options = null)
        {
            _local = Guard.NotNull(local, nameof(local));
            _cloud = Guard.NotNull(cloud, nameof(cloud));
            options ??= new SyncedDataStoreOptions();
            _resolver = options.ConflictResolver ?? new NewestWinsConflictResolver();
            _delay = options.RetryDelay ?? new AwaitableRetryDelay();
            _flushRetry = new CloudRetry(options.FlushRetry ?? new RetryPolicy(), _delay, options.Random);
            _foregroundRetry = new CloudRetry(options.ForegroundRetry ?? RetryPolicy.NoRetry(), _delay, options.Random);
            _clock = options.Clock ?? (() => DateTime.UtcNow);
            _filter = options.SyncFilter ?? (_ => true);
            _uploadOnSave = options.UploadOnSave;
            _journalKey = new DataKey(JournalName, null, options.MetadataFolder);
            foreach (KeyValuePair<Type, List<IMigration>> pair in options.Migrations)
            {
                _migrations[pair.Key] = pair.Value.ToArray();
            }
        }

        #region SYNC

        public void Save<T>(DataKey key, T value)
        {
            if (!TryTrack(key, out string cloudKey))
            {
                _local.Save(key, value);
                return;
            }

            // Serialize first, so a value that cannot be serialized fails before anything is written.
            long ticks = NextTicks(cloudKey);
            string payload = BuildPayload(value, ticks);
            _local.Save(key, value);
            RecordWrite(key, cloudKey, ticks, payload);
        }

        /// <summary>Loads from the local store only. Call <see cref="LoadAsync{T}"/> after signing in to pick up a newer cloud copy.</summary>
        public LoadResult<T> Load<T>(DataKey key) => _local.Load<T>(key);

        public bool TryLoad<T>(DataKey key, out T value) => _local.TryLoad(key, out value);

        public LoadResult<T> LoadInto<T>(DataKey key, T target) where T : class => _local.LoadInto(key, target);

        public bool Exists(DataKey key) => _local.Exists(key);

        /// <returns>True when a local file was deleted. The delete is queued for the cloud either way.</returns>
        public bool Delete(DataKey key)
        {
            bool existed = _local.Delete(key);
            if (TryTrack(key, out string cloudKey))
            {
                RecordDelete(key, cloudKey);
            }

            return existed;
        }

        public IReadOnlyList<DataKey> List(string folder = null, int? slot = null) => _local.List(folder, slot);

        public IReadOnlyList<int> ListSlots(string folder = null) => _local.ListSlots(folder);

        #endregion

        #region ASYNC

        /// <summary>
        /// Saves locally, queues the upload and, with <see cref="SyncedDataStoreOptions.UploadOnSave"/>, tries to upload the queue once.
        /// A cloud failure never throws. Cancelling after the local write throws but keeps the change queued.
        /// </summary>
        public async Awaitable SaveAsync<T>(DataKey key, T value, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryTrack(key, out string cloudKey))
            {
                await _local.SaveAsync(key, value, cancellationToken);
                return;
            }

            long ticks = NextTicks(cloudKey);
            string payload = BuildPayload(value, ticks);
            await _local.SaveAsync(key, value, cancellationToken);
            RecordWrite(key, cloudKey, ticks, payload);
            if (_uploadOnSave)
            {
                await FlushCoreAsync(_foregroundRetry, cancellationToken);
            }
        }

        /// <summary>Loads the newest copy: reconciles with the cloud when it is reachable, otherwise returns the device copy.</summary>
        public async Awaitable<LoadResult<T>> LoadAsync<T>(DataKey key, CancellationToken cancellationToken = default)
        {
            if (!TryTrack(key, out string cloudKey))
            {
                return await _local.LoadAsync<T>(key, cancellationToken);
            }

            LocalAccess<T> access = new()
            {
                Read = token => _local.LoadAsync<T>(key, token),
                Adopt = (text, token) => AdoptAsync<T>(key, text, token)
            };
            return await ReconcileAsync(key, cloudKey, access, cancellationToken);
        }

        public async Awaitable<LoadResult<T>> LoadIntoAsync<T>(DataKey key, T target, CancellationToken cancellationToken = default) where T : class
        {
            Guard.NotNull(target, nameof(target));
            if (!TryTrack(key, out string cloudKey))
            {
                return await _local.LoadIntoAsync(key, target, cancellationToken);
            }

            LocalAccess<T> access = new()
            {
                Read = token => _local.LoadIntoAsync(key, target, token),
                Adopt = (text, token) => AdoptIntoAsync(key, text, target, token)
            };
            return await ReconcileAsync(key, cloudKey, access, cancellationToken);
        }

        /// <summary>True when the device or the cloud holds the key. A cloud failure counts as absent there.</summary>
        public async Awaitable<bool> ExistsAsync(DataKey key, CancellationToken cancellationToken = default)
        {
            if (await _local.ExistsAsync(key, cancellationToken))
            {
                return true;
            }

            if (!TryTrack(key, out string cloudKey) || IsDeleted(cloudKey))
            {
                return false;
            }

            IReadOnlyList<CloudSaveKeyInfo> cloudKeys = await ListCloudKeysAsync(cancellationToken);
            if (cloudKeys == null)
            {
                return false;
            }

            foreach (CloudSaveKeyInfo info in cloudKeys)
            {
                if (string.Equals(info.Key, cloudKey, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Deletes locally, queues the delete and, with <see cref="SyncedDataStoreOptions.UploadOnSave"/>, tries to upload the queue once.</summary>
        public async Awaitable<bool> DeleteAsync(DataKey key, CancellationToken cancellationToken = default)
        {
            bool existed = await _local.DeleteAsync(key, cancellationToken);
            if (!TryTrack(key, out string cloudKey))
            {
                return existed;
            }

            RecordDelete(key, cloudKey);
            if (_uploadOnSave)
            {
                await FlushCoreAsync(_foregroundRetry, cancellationToken);
            }

            return existed;
        }

        /// <summary>Lists the device keys plus keys that exist only in the cloud, for example after installing on a new device.</summary>
        public async Awaitable<IReadOnlyList<DataKey>> ListAsync(string folder = null, int? slot = null, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<DataKey> localKeys = await _local.ListAsync(folder, slot, cancellationToken);
            List<DataKey> keys = new(localKeys);
            HashSet<DataKey> seen = new(localKeys);
            IReadOnlyList<CloudSaveKeyInfo> cloudKeys = await ListCloudKeysAsync(cancellationToken);
            if (cloudKeys != null)
            {
                string wantedFolder = string.IsNullOrEmpty(folder) ? null : folder;
                foreach (CloudSaveKeyInfo info in cloudKeys)
                {
                    if (CloudKeyMapper.TryParse(info.Key, out DataKey parsed)
                        && string.Equals(parsed.Folder, wantedFolder, StringComparison.Ordinal)
                        && parsed.Slot == slot
                        && !IsDeleted(info.Key)
                        && seen.Add(parsed))
                    {
                        keys.Add(parsed);
                    }
                }
            }

            keys.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return keys;
        }

        public async Awaitable<IReadOnlyList<int>> ListSlotsAsync(string folder = null, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<int> localSlots = await _local.ListSlotsAsync(folder, cancellationToken);
            SortedSet<int> slots = new(localSlots);
            IReadOnlyList<CloudSaveKeyInfo> cloudKeys = await ListCloudKeysAsync(cancellationToken);
            if (cloudKeys != null)
            {
                string wantedFolder = string.IsNullOrEmpty(folder) ? null : folder;
                foreach (CloudSaveKeyInfo info in cloudKeys)
                {
                    if (CloudKeyMapper.TryParse(info.Key, out DataKey parsed)
                        && parsed.Slot.HasValue
                        && string.Equals(parsed.Folder, wantedFolder, StringComparison.Ordinal)
                        && !IsDeleted(info.Key))
                    {
                        slots.Add(parsed.Slot.Value);
                    }
                }
            }

            return new List<int>(slots);
        }

        #endregion

        /// <summary>Uploads every queued change in the order it was made, retrying transient failures with backoff. A cloud failure never throws.</summary>
        public Awaitable<SyncStatus> FlushAsync(CancellationToken cancellationToken = default)
        {
            return FlushCoreAsync(_flushRetry, cancellationToken);
        }

        /// <summary>
        /// Flushes every <paramref name="interval"/> until cancelled. Own the loop: start it from a component and pass its destroy token,
        /// then await it or observe it in a try/catch for <see cref="OperationCanceledException"/>.
        /// </summary>
        public Awaitable RunSyncLoopAsync(TimeSpan interval, CancellationToken cancellationToken)
        {
            if (interval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(interval), interval, "The interval must be greater than zero.");
            }

            return SyncLoopAsync(interval, cancellationToken);
        }

        private async Awaitable SyncLoopAsync(TimeSpan interval, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await FlushAsync(cancellationToken);
                await _delay.DelayAsync(interval, cancellationToken);
            }
        }

        private SyncJournal Journal
        {
            get
            {
                if (_journal == null)
                {
                    LoadResult<SyncJournal> loaded = _local.Load<SyncJournal>(_journalKey);
                    _journal = loaded.Ok && loaded.Value != null ? loaded.Value : new SyncJournal();
                }

                return _journal;
            }
        }

        private bool TryTrack(DataKey key, out string cloudKey)
        {
            cloudKey = null;
            if (!key.IsValid || !_filter(key))
            {
                return false;
            }

            cloudKey = CloudKeyMapper.ToCloudKey(key);
            return true;
        }

        private bool IsDeleted(string cloudKey)
        {
            SyncEntry entry = Journal.Find(cloudKey);
            return entry != null && entry.state == SyncEntryState.PendingDelete;
        }

        // Save times must grow with every save of a key, even when the clock repeats or steps back.
        private long NextTicks(string cloudKey)
        {
            long now = _clock().Ticks;
            SyncEntry entry = Journal.Find(cloudKey);
            return entry != null && entry.savedAtUtcTicks >= now ? entry.savedAtUtcTicks + 1 : now;
        }

        private void RecordWrite(DataKey key, string cloudKey, long ticks, string payload)
        {
            Journal.RecordWrite(key, cloudKey, ticks, payload);
            SaveJournal();
            UpdateStatus();
        }

        private void RecordDelete(DataKey key, string cloudKey)
        {
            Journal.RecordDelete(key, cloudKey, NextTicks(cloudKey));
            SaveJournal();
            UpdateStatus();
        }

        private void SaveJournal() => _local.Save(_journalKey, Journal);

        private async Awaitable<IReadOnlyList<CloudSaveKeyInfo>> ListCloudKeysAsync(CancellationToken cancellationToken)
        {
            CloudResult<IReadOnlyList<CloudSaveKeyInfo>> result = await _foregroundRetry.RunAsync(token => _cloud.ListKeysAsync(token), cancellationToken);
            if (!result.IsSuccess)
            {
                RecordCloudFailure(result.Error);
                return null;
            }

            RecordCloudSuccess();
            return result.Value;
        }
    }
}
