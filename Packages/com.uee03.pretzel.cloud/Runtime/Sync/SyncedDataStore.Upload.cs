using System;
using System.Threading;
using Pretzel.Data;
using UnityEngine;

namespace Pretzel.Cloud
{
    public sealed partial class SyncedDataStore
    {
        private enum UploadFlow
        {
            /// <summary>The entry was changed so the upload should be tried again now.</summary>
            Retry,

            /// <summary>The entry is settled; go on with the next one.</summary>
            Continue,

            /// <summary>The cloud is not usable right now; leave the rest of the queue for later.</summary>
            Stop
        }

        #region UPLOAD

        private async Awaitable<SyncStatus> FlushCoreAsync(CloudRetry retry, CancellationToken cancellationToken)
        {
            if (Journal.CountPending() == 0)
            {
                return Status;
            }

            await _gate.WaitAsync(cancellationToken);
            try
            {
                BeginSync();
                try
                {
                    foreach (SyncEntry entry in Journal.PendingInOrder())
                    {
                        // Stop at the first transient failure: later changes must not overtake an earlier one.
                        if (!await UploadEntryAsync(entry, retry, cancellationToken))
                        {
                            break;
                        }
                    }
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

            return Status;
        }

        // Call with the gate held. Returns false when the queue should stop, true when the entry is settled.
        private async Awaitable<bool> UploadEntryAsync(SyncEntry entry, CloudRetry retry, CancellationToken cancellationToken)
        {
            for (int round = 0; round < 2; round++)
            {
                if (!entry.IsPendingUpload())
                {
                    return true;
                }

                long sequence = entry.sequence;
                long ticks = entry.savedAtUtcTicks;
                bool isDelete = entry.state == SyncEntryState.PendingDelete;
                string cloudKey = entry.cloudKey;
                string payload = entry.payload;
                string writeLock = string.IsNullOrEmpty(entry.writeLock) ? null : entry.writeLock;

                CloudResult<string> result;
                if (isDelete)
                {
                    CloudResult deleted = await retry.RunAsync(token => _cloud.DeleteAsync(cloudKey, writeLock, token), cancellationToken);
                    result = deleted.IsSuccess ? CloudResult<string>.Success(string.Empty) : CloudResult<string>.Failure(deleted.Error);
                }
                else
                {
                    result = await retry.RunAsync(token => _cloud.SaveAsync(cloudKey, payload, writeLock, token), cancellationToken);
                }

                bool settled = result.IsSuccess || (isDelete && result.Error.Kind == CloudErrorKind.NotFound);
                if (settled)
                {
                    CompleteUpload(entry, sequence, ticks, isDelete, result.IsSuccess ? result.Value : string.Empty);
                    return true;
                }

                CloudError error = result.Error;
                if (error.Kind == CloudErrorKind.Conflict && round == 0)
                {
                    UploadFlow flow = await ResolveUploadConflictAsync(entry, retry, cancellationToken);
                    if (flow == UploadFlow.Retry)
                    {
                        continue;
                    }

                    return flow == UploadFlow.Continue;
                }

                if (error.IsTransient || error.NeedsSignIn || error.Kind == CloudErrorKind.Unknown || error.Kind == CloudErrorKind.Conflict)
                {
                    RecordCloudFailure(error);
                    return false;
                }

                // The service refused this key for good. Park it so it cannot block the keys behind it.
                RecordCloudSuccess();
                Journal.MarkFailed(entry, error);
                SaveJournal();
                UpdateStatus();
                return true;
            }

            return false;
        }

        private void CompleteUpload(SyncEntry entry, long sequence, long ticks, bool wasDelete, string newWriteLock)
        {
            if (entry.sequence == sequence)
            {
                if (wasDelete)
                {
                    Journal.Remove(entry);
                }
                else
                {
                    Journal.MarkSynced(entry, ticks, newWriteLock);
                }
            }
            else
            {
                // The player changed the key while it was uploading. The newer change stays queued on top of the new cloud state.
                entry.writeLock = wasDelete ? string.Empty : newWriteLock ?? string.Empty;
            }

            SaveJournal();
            RecordCloudSuccess();
        }

        private async Awaitable<UploadFlow> ResolveUploadConflictAsync(SyncEntry entry, CloudRetry retry, CancellationToken cancellationToken)
        {
            string cloudKey = entry.cloudKey;
            CloudResult<CloudSaveItem> cloud = await retry.RunAsync(token => _cloud.LoadAsync(cloudKey, token), cancellationToken);
            CloudSaveItem item = null;
            CloudPayload payload = null;
            if (cloud.IsSuccess)
            {
                item = cloud.Value;
                if (!TryParsePayload(item.Value, out payload))
                {
                    payload = null;
                }
            }
            else if (cloud.Error.Kind != CloudErrorKind.NotFound)
            {
                RecordCloudFailure(cloud.Error);
                return UploadFlow.Stop;
            }

            long cloudTicks = payload != null ? payload.savedAtUtcTicks : 0L;
            SyncAction action = SyncPlanner.Plan(entry, entry.state != SyncEntryState.PendingDelete, item != null, cloudTicks, entry.ToDataKey(), _resolver, out bool conflict);
            if (conflict)
            {
                _resolvedConflicts++;
            }

            switch (action)
            {
                case SyncAction.PullCloud:
                    // Typed code applies the download on the next load.
                    Journal.MarkCloudNewer(entry, cloudTicks, item.WriteLock);
                    SaveJournal();
                    UpdateStatus();
                    return UploadFlow.Continue;
                case SyncAction.PushLocal:
                    if (item != null && entry.state == SyncEntryState.PendingWrite && cloudTicks >= entry.savedAtUtcTicks)
                    {
                        RetimePayload(entry, cloudTicks + 1);
                    }

                    entry.writeLock = item != null ? item.WriteLock ?? string.Empty : string.Empty;
                    SaveJournal();
                    return UploadFlow.Retry;
                default:
                    if (entry.state == SyncEntryState.PendingDelete)
                    {
                        Journal.Remove(entry);
                    }
                    else if (item != null)
                    {
                        Journal.MarkSynced(entry, cloudTicks, item.WriteLock);
                    }

                    SaveJournal();
                    RecordCloudSuccess();
                    return UploadFlow.Continue;
            }
        }

        // Rewrites the save time inside a queued payload, so a device copy that wins a conflict is newer than the cloud copy it replaces.
        private void RetimePayload(SyncEntry entry, long ticks)
        {
            if (!TryParsePayload(entry.payload, out CloudPayload payload))
            {
                return;
            }

            payload.savedAtUtcTicks = ticks;
            entry.payload = Serializer.Serialize(payload, false);
            entry.savedAtUtcTicks = ticks;
        }

        #endregion

        #region STATUS

        private void BeginSync()
        {
            _syncingDepth++;
            UpdateStatus();
        }

        private void EndSync()
        {
            _syncingDepth--;
            UpdateStatus();
        }

        private void RecordCloudSuccess()
        {
            _lastError = null;
            _lastSyncedUtc = _clock();
            UpdateStatus();
        }

        private void RecordCloudFailure(CloudError error)
        {
            _lastError = error;
            UpdateStatus();
        }

        private SyncStatus BuildStatus()
        {
            return SyncStatus.From(_syncingDepth > 0, Journal.CountPending(), Journal.CountFailed(), _lastError, _lastSyncedUtc, _resolvedConflicts);
        }

        private void UpdateStatus()
        {
            SyncStatus previous = _status;
            SyncStatus next = BuildStatus();
            _status = next;
            if (previous == null || !SameStatus(previous, next))
            {
                StatusChanged?.Invoke(next);
            }
        }

        private static bool SameStatus(SyncStatus a, SyncStatus b)
        {
            return a.State == b.State
                   && a.PendingCount == b.PendingCount
                   && a.FailedCount == b.FailedCount
                   && ReferenceEquals(a.LastError, b.LastError)
                   && a.LastSyncedUtc == b.LastSyncedUtc
                   && a.ResolvedConflicts == b.ResolvedConflicts;
        }

        #endregion

        #region PAYLOAD

        private string BuildPayload<T>(T value, long ticks)
        {
            CloudPayload payload = new()
            {
                savedAtUtcTicks = ticks,
                schemaVersion = SchemaVersionOf(typeof(T)),
                data = Serializer.Serialize(value, false)
            };
            return Serializer.Serialize(payload, false);
        }

        private bool TryParsePayload(string text, out CloudPayload payload)
        {
            payload = null;
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            try
            {
                CloudPayload parsed = Serializer.Deserialize<CloudPayload>(text);
                if (parsed == null || parsed.format != CloudPayload.CurrentFormat || parsed.data == null)
                {
                    return false;
                }

                payload = parsed;
                return true;
            }
            catch (Exception ex) when (CloudSerialization.IsFailure(ex))
            {
                return false;
            }
        }

        // Applies the migrations registered for the type, like the local store does for files.
        private bool TryMigrate(Type type, CloudPayload payload, out string text, out CloudError error)
        {
            text = payload.data;
            error = null;
            int current = SchemaVersionOf(type);
            int version = payload.schemaVersion;
            if (version == current)
            {
                return true;
            }

            if (version > current)
            {
                error = new CloudError(CloudErrorKind.Serialization, $"{type.Name} was saved with schema {version}, but this build supports up to {current}.");
                return false;
            }

            _migrations.TryGetValue(type, out IMigration[] steps);
            try
            {
                while (version < current)
                {
                    int from = version;
                    IMigration step = steps == null ? null : Array.Find(steps, m => m.FromVersion == from);
                    if (step == null)
                    {
                        error = new CloudError(CloudErrorKind.Serialization, $"No migration registered for {type.Name} from schema {version} to {current}.");
                        return false;
                    }

                    text = step.Migrate(text);
                    version = step.ToVersion;
                }
            }
            catch (Exception ex) when (CloudSerialization.IsFailure(ex))
            {
                error = new CloudError(CloudErrorKind.Serialization, $"Migrating {type.Name} failed: {ex.Message}");
                return false;
            }

            return true;
        }

        private static int SchemaVersionOf(Type type)
        {
            SchemaVersionAttribute attribute = (SchemaVersionAttribute)Attribute.GetCustomAttribute(type, typeof(SchemaVersionAttribute), false);
            return attribute != null ? attribute.Version : 0;
        }

        #endregion
    }
}
