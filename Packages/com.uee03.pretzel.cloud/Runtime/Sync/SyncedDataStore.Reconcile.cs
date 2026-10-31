using System;
using System.Threading;
using Pretzel.Data;
using UnityEngine;

namespace Pretzel.Cloud
{
    public sealed partial class SyncedDataStore
    {
        // How a load reads and replaces the device copy. Load and LoadInto differ only in this.
        private sealed class LocalAccess<T>
        {
            public Func<CancellationToken, Awaitable<LoadResult<T>>> Read;
            public Func<string, CancellationToken, Awaitable<LoadResult<T>>> Adopt;
        }

        private async Awaitable<LoadResult<T>> ReconcileAsync<T>(DataKey key, string cloudKey, LocalAccess<T> access, CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                BeginSync();
                try
                {
                    return await ReconcileCoreAsync(key, cloudKey, access, cancellationToken);
                }
                finally
                {
                    EndSync();
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Awaitable<LoadResult<T>> ReconcileCoreAsync<T>(DataKey key, string cloudKey, LocalAccess<T> access, CancellationToken cancellationToken)
        {
            long observed = SequenceOf(Journal.Find(cloudKey));
            LoadResult<T> local = await access.Read(cancellationToken);

            // Data this build cannot read (newer schema, wrong key) must not be replaced by a cloud copy.
            if (local.Status == DataStatus.Corrupt && local.ErrorCode != DataError.InvalidData)
            {
                return local;
            }

            CloudResult<CloudSaveItem> cloud = await _foregroundRetry.RunAsync(token => _cloud.LoadAsync(cloudKey, token), cancellationToken);
            CloudSaveItem item = null;
            CloudPayload payload = null;
            if (cloud.IsSuccess)
            {
                item = cloud.Value;
                if (!TryParsePayload(item.Value, out payload))
                {
                    RecordCloudFailure(new CloudError(CloudErrorKind.Serialization, $"The cloud copy of {key} cannot be read by this build."));
                    return local;
                }
            }
            else if (cloud.Error.Kind != CloudErrorKind.NotFound)
            {
                RecordCloudFailure(cloud.Error);
                return local;
            }

            RecordCloudSuccess();
            SyncEntry entry = Journal.Find(cloudKey);
            if (SequenceOf(entry) != observed)
            {
                // The player saved while this load was waiting, so the device copy already moved on.
                return await access.Read(cancellationToken);
            }

            bool cloudExists = item != null;
            long cloudTicks = payload != null ? payload.savedAtUtcTicks : 0L;
            SyncAction action = SyncPlanner.Plan(entry, local.Ok, cloudExists, cloudTicks, key, _resolver, out bool conflict);
            if (conflict)
            {
                _resolvedConflicts++;
            }

            switch (action)
            {
                case SyncAction.PullCloud:
                    return await PullAsync(key, cloudKey, access, local, item, payload, observed, cancellationToken);
                case SyncAction.PushLocal:
                    return await PushAsync(key, cloudKey, entry, local, item, cloudTicks, cancellationToken);
                default:
                    MarkInSync(entry, item, cloudTicks);
                    return local;
            }
        }

        private void MarkInSync(SyncEntry entry, CloudSaveItem item, long cloudTicks)
        {
            if (entry != null)
            {
                bool changed = false;
                if (entry.state == SyncEntryState.PendingDelete)
                {
                    Journal.Remove(entry);
                    changed = true;
                }
                else if (item != null && (entry.state != SyncEntryState.Synced
                                          || entry.savedAtUtcTicks != cloudTicks
                                          || !string.Equals(entry.writeLock, item.WriteLock ?? string.Empty, StringComparison.Ordinal)))
                {
                    Journal.MarkSynced(entry, cloudTicks, item.WriteLock);
                    changed = true;
                }

                if (changed)
                {
                    SaveJournal();
                }
            }

            UpdateStatus();
        }

        private async Awaitable<LoadResult<T>> PullAsync<T>(DataKey key, string cloudKey, LocalAccess<T> access, LoadResult<T> local, CloudSaveItem item, CloudPayload payload, long observed, CancellationToken cancellationToken)
        {
            if (!TryMigrate(typeof(T), payload, out string text, out CloudError migrationError))
            {
                RecordCloudFailure(migrationError);
                return local;
            }

            LoadResult<T> adopted = await access.Adopt(text, cancellationToken);
            if (!adopted.Ok)
            {
                RecordCloudFailure(new CloudError(CloudErrorKind.Serialization, $"The cloud copy of {key} could not be applied: {adopted.Error}"));
                return local;
            }

            // A save made while the download was being applied stays queued; it is newer than what was just downloaded.
            if (SequenceOf(Journal.Find(cloudKey)) == observed)
            {
                Journal.TrackSynced(key, cloudKey, payload.savedAtUtcTicks, item.WriteLock);
                SaveJournal();
            }

            UpdateStatus();
            return adopted;
        }

        private async Awaitable<LoadResult<T>> PushAsync<T>(DataKey key, string cloudKey, SyncEntry entry, LoadResult<T> local, CloudSaveItem item, long cloudTicks, CancellationToken cancellationToken)
        {
            if (entry != null && entry.state == SyncEntryState.PendingDelete)
            {
                entry.writeLock = item != null ? item.WriteLock : string.Empty;
                SaveJournal();
                await UploadEntryAsync(entry, _foregroundRetry, cancellationToken);
                return local;
            }

            if (!local.Ok)
            {
                return local;
            }

            // A device copy that wins must end up newer than the cloud copy it replaces.
            long ticks = entry != null && entry.savedAtUtcTicks > 0 ? entry.savedAtUtcTicks : NextTicks(cloudKey);
            if (item != null && cloudTicks >= ticks)
            {
                ticks = cloudTicks + 1;
            }

            if (entry == null || entry.state != SyncEntryState.PendingWrite || entry.savedAtUtcTicks != ticks)
            {
                entry = Journal.RecordWrite(key, cloudKey, ticks, BuildPayload(local.Value, ticks));
            }

            if (item != null)
            {
                entry.writeLock = item.WriteLock;
            }

            SaveJournal();
            UpdateStatus();
            await UploadEntryAsync(entry, _foregroundRetry, cancellationToken);
            return local;
        }

        private async Awaitable<LoadResult<T>> AdoptAsync<T>(DataKey key, string text, CancellationToken cancellationToken)
        {
            T value;
            try
            {
                value = Serializer.Deserialize<T>(text);
            }
            catch (Exception ex) when (CloudSerialization.IsFailure(ex))
            {
                return LoadResult<T>.Corrupt(ex.Message);
            }

            if (value == null)
            {
                return LoadResult<T>.Corrupt("The cloud copy contained no data.");
            }

            await _local.SaveAsync(key, value, cancellationToken);
            return LoadResult<T>.Success(value);
        }

        private async Awaitable<LoadResult<T>> AdoptIntoAsync<T>(DataKey key, string text, T target, CancellationToken cancellationToken) where T : class
        {
            try
            {
                Serializer.Populate(text, target);
            }
            catch (Exception ex) when (CloudSerialization.IsFailure(ex))
            {
                return LoadResult<T>.Corrupt(ex.Message);
            }

            await _local.SaveAsync(key, target, cancellationToken);
            return LoadResult<T>.Success(target);
        }

        private static long SequenceOf(SyncEntry entry) => entry != null ? entry.sequence : -1L;
    }
}
