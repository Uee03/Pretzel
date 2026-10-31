using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Pretzel.Cloud
{
    /// <summary>
    /// Per-player key/value storage in the cloud. Values are text; <see cref="SyncedDataStore"/> puts serialized Pretzel Data in them.
    /// Failures are results, cancellation throws. All members must be used from the main thread.
    /// </summary>
    public interface ICloudSaveService
    {
        /// <summary>Reads one key. A missing key is <see cref="CloudErrorKind.NotFound"/>.</summary>
        Awaitable<CloudResult<CloudSaveItem>> LoadAsync(string key, CancellationToken cancellationToken = default);

        /// <param name="writeLock">The write lock from the last read, or null to overwrite without checking.</param>
        /// <returns>The new write lock.</returns>
        Awaitable<CloudResult<string>> SaveAsync(string key, string value, string writeLock = null, CancellationToken cancellationToken = default);

        /// <param name="writeLock">The write lock from the last read, or null to delete without checking. Deleting a missing key succeeds.</param>
        Awaitable<CloudResult> DeleteAsync(string key, string writeLock = null, CancellationToken cancellationToken = default);

        /// <summary>Lists every key of the signed-in player, following pages.</summary>
        Awaitable<CloudResult<IReadOnlyList<CloudSaveKeyInfo>>> ListKeysAsync(CancellationToken cancellationToken = default);
    }
}
