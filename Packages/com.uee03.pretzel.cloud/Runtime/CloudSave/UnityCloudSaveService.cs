using System;
using System.Collections.Generic;
using System.Threading;
using Pretzel.Core;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Internal;
using Unity.Services.CloudSave.Models;
using UnityEngine;
using PlayerDeleteOptions = Unity.Services.CloudSave.Models.Data.Player.DeleteOptions;

namespace Pretzel.Cloud.CloudSave
{
    /// <summary>
    /// <see cref="Pretzel.Cloud.ICloudSaveService"/> on Cloud Save player data (default access class). Values are stored as strings.
    /// Unity Services must be initialized and the player signed in. The SDK calls take no cancellation token, so cancelling stops the wait
    /// but a request already sent may still reach the service. Use from the main thread.
    /// </summary>
    public sealed class UnityCloudSaveService : Pretzel.Cloud.ICloudSaveService
    {
        private readonly Func<IPlayerDataService> _playerData;

        /// <summary>Uses <c>CloudSaveService.Instance.Data.Player</c>, resolved on every call so it works once Unity Services are initialized.</summary>
        public UnityCloudSaveService()
            : this(() => CloudSaveService.Instance.Data.Player)
        {
        }

        /// <param name="playerData">Supplies the SDK's player data service. Use it to inject a stub in tests.</param>
        public UnityCloudSaveService(Func<IPlayerDataService> playerData)
        {
            _playerData = Guard.NotNull(playerData, nameof(playerData));
        }

        public async Awaitable<CloudResult<CloudSaveItem>> LoadAsync(string key, CancellationToken cancellationToken = default)
        {
            Guard.NotNullOrEmpty(key, nameof(key));
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                HashSet<string> keys = new(StringComparer.Ordinal) { key };
                Dictionary<string, Item> items = await _playerData().LoadAsync(keys).WithCancellationAsync(cancellationToken);
                if (!items.TryGetValue(key, out Item item))
                {
                    return CloudResult<CloudSaveItem>.Failure(CloudErrorKind.NotFound, $"No Cloud Save item '{key}'.");
                }

                string value = item.Value != null ? item.Value.GetAsString() : string.Empty;
                return CloudResult<CloudSaveItem>.Success(new CloudSaveItem(item.Key, value, item.WriteLock, ToUtc(item.Modified)));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return CloudResult<CloudSaveItem>.Failure(UnityCloudSaveErrors.Map(ex));
            }
        }

        public async Awaitable<CloudResult<string>> SaveAsync(string key, string value, string writeLock = null, CancellationToken cancellationToken = default)
        {
            Guard.NotNullOrEmpty(key, nameof(key));
            Guard.NotNull(value, nameof(value));
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                Dictionary<string, SaveItem> data = new(StringComparer.Ordinal) { [key] = new SaveItem(value, writeLock) };
                Dictionary<string, string> locks = await _playerData().SaveAsync(data).WithCancellationAsync(cancellationToken);
                locks.TryGetValue(key, out string newLock);
                return CloudResult<string>.Success(newLock ?? string.Empty);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return CloudResult<string>.Failure(UnityCloudSaveErrors.Map(ex));
            }
        }

        public async Awaitable<CloudResult> DeleteAsync(string key, string writeLock = null, CancellationToken cancellationToken = default)
        {
            Guard.NotNullOrEmpty(key, nameof(key));
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                PlayerDeleteOptions options = new() { WriteLock = writeLock };
                await _playerData().DeleteAsync(key, options).WithCancellationAsync(cancellationToken);
                return CloudResult.Success();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return CloudResult.Failure(UnityCloudSaveErrors.Map(ex));
            }
        }

        public async Awaitable<CloudResult<IReadOnlyList<CloudSaveKeyInfo>>> ListKeysAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                List<ItemKey> keys = await _playerData().ListAllKeysAsync().WithCancellationAsync(cancellationToken);
                List<CloudSaveKeyInfo> infos = new(keys.Count);
                foreach (ItemKey itemKey in keys)
                {
                    infos.Add(new CloudSaveKeyInfo(itemKey.Key, itemKey.WriteLock, ToUtc(itemKey.Modified)));
                }

                return CloudResult<IReadOnlyList<CloudSaveKeyInfo>>.Success(infos);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return CloudResult<IReadOnlyList<CloudSaveKeyInfo>>.Failure(UnityCloudSaveErrors.Map(ex));
            }
        }

        private static DateTime? ToUtc(DateTime? value)
        {
            return value.HasValue ? value.Value.ToUniversalTime() : null;
        }
    }
}
