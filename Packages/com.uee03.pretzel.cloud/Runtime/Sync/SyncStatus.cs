using System;

namespace Pretzel.Cloud
{
    /// <summary>An immutable snapshot of where syncing stands, for a status label or a save indicator.</summary>
    public sealed class SyncStatus
    {
        public SyncState State { get; }

        /// <summary>Keys with an upload or delete waiting.</summary>
        public int PendingCount { get; }

        /// <summary>Keys the service rejected for good.</summary>
        public int FailedCount { get; }

        /// <summary>When a cloud call last succeeded, in UTC, or null.</summary>
        public DateTime? LastSyncedUtc { get; }

        /// <summary>The error behind <see cref="State"/>, or null.</summary>
        public CloudError LastError { get; }

        /// <summary>Conflicts the resolver has decided since the store was created.</summary>
        public int ResolvedConflicts { get; }

        public SyncStatus(SyncState state, int pendingCount, int failedCount, DateTime? lastSyncedUtc, CloudError lastError, int resolvedConflicts)
        {
            State = state;
            PendingCount = pendingCount;
            FailedCount = failedCount;
            LastSyncedUtc = lastSyncedUtc;
            LastError = lastError;
            ResolvedConflicts = resolvedConflicts;
        }

        /// <summary>Derives the state from the counts and the last error.</summary>
        public static SyncStatus From(bool syncing, int pendingCount, int failedCount, CloudError lastError, DateTime? lastSyncedUtc, int resolvedConflicts)
        {
            SyncState state;
            if (syncing)
            {
                state = SyncState.Syncing;
            }
            else if (lastError != null && lastError.NeedsSignIn)
            {
                state = SyncState.NotSignedIn;
            }
            else if (lastError != null && (lastError.Kind == CloudErrorKind.Offline || lastError.Kind == CloudErrorKind.Timeout))
            {
                state = SyncState.Offline;
            }
            else if (lastError != null || failedCount > 0)
            {
                state = SyncState.Error;
            }
            else
            {
                state = pendingCount > 0 ? SyncState.Pending : SyncState.Synced;
            }

            return new SyncStatus(state, pendingCount, failedCount, lastSyncedUtc, lastError, resolvedConflicts);
        }

        public override string ToString() => $"{State} (pending {PendingCount}, failed {FailedCount})";
    }
}
