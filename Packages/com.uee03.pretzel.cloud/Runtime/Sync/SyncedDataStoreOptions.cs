using System;
using System.Collections.Generic;
using Pretzel.Core;
using Pretzel.Data;

namespace Pretzel.Cloud
{
    /// <summary>Settings for a <see cref="SyncedDataStore"/>. The store takes a snapshot when it is constructed.</summary>
    public sealed class SyncedDataStoreOptions
    {
        private readonly Dictionary<Type, List<IMigration>> _migrations = new();

        /// <summary>Decides conflicts. Defaults to <see cref="NewestWinsConflictResolver"/>.</summary>
        public IConflictResolver ConflictResolver { get; set; }

        /// <summary>Retries for <see cref="SyncedDataStore.FlushAsync"/> and the sync loop. Defaults to three attempts with backoff.</summary>
        public RetryPolicy FlushRetry { get; set; }

        /// <summary>
        /// Retries for calls a player is waiting on: <c>SaveAsync</c>, <c>DeleteAsync</c>, <c>LoadAsync</c> and listing.
        /// Defaults to one attempt, so a bad connection never stalls the game; the queue retries later.
        /// </summary>
        public RetryPolicy ForegroundRetry { get; set; }

        /// <summary>Waits between retries. Defaults to <see cref="AwaitableRetryDelay"/>; use a zero delay in tests.</summary>
        public IRetryDelay RetryDelay { get; set; }

        /// <summary>Returns 0 to 1 for retry jitter. Defaults to <see cref="System.Random"/>.</summary>
        public Func<double> Random { get; set; }

        /// <summary>The UTC clock used for save times. Defaults to <see cref="DateTime.UtcNow"/>.</summary>
        public Func<DateTime> Clock { get; set; }

        /// <summary>Returns false for keys that stay on the device only, such as thumbnails or per-device settings. Defaults to syncing every key.</summary>
        public Func<DataKey, bool> SyncFilter { get; set; }

        /// <summary>The local folder for the journal. It sits next to your data and is never listed with it.</summary>
        public string MetadataFolder { get; set; } = "PretzelCloud";

        /// <summary>True to try an upload at the end of <c>SaveAsync</c> and <c>DeleteAsync</c>. False queues only, for you to flush on your own schedule.</summary>
        public bool UploadOnSave { get; set; } = true;

        internal Dictionary<Type, List<IMigration>> Migrations => _migrations;

        /// <summary>
        /// Registers a migration for cloud payloads of <typeparamref name="T"/>, the same way <see cref="DataStoreOptions.AddMigration{T}"/> does for files.
        /// Register the steps you gave the local store, so a cloud save written by an older build still loads.
        /// </summary>
        public SyncedDataStoreOptions AddMigration<T>(IMigration migration)
        {
            Guard.NotNull(migration, nameof(migration));
            if (migration.FromVersion < 0 || migration.ToVersion <= migration.FromVersion)
            {
                throw new ArgumentException($"A migration must go from version 0 or later to a higher version, not {migration.FromVersion} to {migration.ToVersion}.", nameof(migration));
            }

            if (!_migrations.TryGetValue(typeof(T), out List<IMigration> list))
            {
                list = new List<IMigration>();
                _migrations[typeof(T)] = list;
            }

            if (list.Exists(m => m.FromVersion == migration.FromVersion))
            {
                throw new ArgumentException($"{typeof(T).Name} already has a migration from version {migration.FromVersion}.", nameof(migration));
            }

            list.Add(migration);
            return this;
        }
    }
}
