using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Pretzel.Core;
using UnityEngine;

namespace Pretzel.Cloud.InMemory
{
    /// <summary>
    /// An in-memory <see cref="ICloudSaveService"/> with real write-lock semantics, for tests and for developing without a backend.
    /// Nothing is persisted. Not thread-safe; use it from the main thread like the real services.
    /// </summary>
    public sealed class InMemoryCloudSaveService : ICloudSaveService
    {
        private sealed class Stored
        {
            public string Value;
            public long Lock;
            public DateTime ModifiedUtc;
        }

        private readonly Dictionary<string, Stored> _items = new(StringComparer.Ordinal);
        private readonly Func<DateTime> _clock;
        private long _lockCounter;

        public CloudFaults Faults { get; } = new();

        public int LoadCount { get; private set; }

        public int SaveCount { get; private set; }

        public int DeleteCount { get; private set; }

        public int ListCount { get; private set; }

        /// <summary>Runs with the key at the start of every <see cref="SaveAsync"/>, so a test can act while an upload is in flight.</summary>
        public Action<string> SaveHook { get; set; }

        /// <summary>Runs with the key at the start of every <see cref="LoadAsync"/>, so a test can act while a download is in flight.</summary>
        public Action<string> LoadHook { get; set; }

        public InMemoryCloudSaveService(Func<DateTime> clock = null)
        {
            _clock = clock ?? (() => DateTime.UtcNow);
        }

        /// <summary>The keys currently stored.</summary>
        public IReadOnlyList<string> Keys => new List<string>(_items.Keys);

        /// <summary>Stores a value directly, as if another device had saved it. Returns the new write lock.</summary>
        public string Seed(string key, string value)
        {
            Guard.NotNullOrEmpty(key, nameof(key));
            Stored stored = new() { Value = value, Lock = ++_lockCounter, ModifiedUtc = _clock() };
            _items[key] = stored;
            return stored.Lock.ToString(CultureInfo.InvariantCulture);
        }

        public bool TryGetValue(string key, out string value)
        {
            bool found = _items.TryGetValue(key, out Stored stored);
            value = found ? stored.Value : null;
            return found;
        }

        public async Awaitable<CloudResult<CloudSaveItem>> LoadAsync(string key, CancellationToken cancellationToken = default)
        {
            await Awaitable.MainThreadAsync();
            cancellationToken.ThrowIfCancellationRequested();
            LoadCount++;
            LoadHook?.Invoke(key);
            CloudError fault = Faults.Next();
            if (fault != null)
            {
                return CloudResult<CloudSaveItem>.Failure(fault);
            }

            if (!_items.TryGetValue(key, out Stored stored))
            {
                return CloudResult<CloudSaveItem>.Failure(CloudErrorKind.NotFound, $"No item '{key}'.");
            }

            return CloudResult<CloudSaveItem>.Success(new CloudSaveItem(key, stored.Value, stored.Lock.ToString(CultureInfo.InvariantCulture), stored.ModifiedUtc));
        }

        public async Awaitable<CloudResult<string>> SaveAsync(string key, string value, string writeLock = null, CancellationToken cancellationToken = default)
        {
            await Awaitable.MainThreadAsync();
            cancellationToken.ThrowIfCancellationRequested();
            SaveCount++;
            SaveHook?.Invoke(key);
            CloudError fault = Faults.Next();
            if (fault != null)
            {
                return CloudResult<string>.Failure(fault);
            }

            if (string.IsNullOrEmpty(key) || value == null)
            {
                return CloudResult<string>.Failure(CloudErrorKind.InvalidRequest, "A key and a value are required.");
            }

            if (writeLock != null && !LockMatches(key, writeLock))
            {
                return CloudResult<string>.Failure(CloudErrorKind.Conflict, $"The write lock for '{key}' does not match.");
            }

            return CloudResult<string>.Success(Seed(key, value));
        }

        public async Awaitable<CloudResult> DeleteAsync(string key, string writeLock = null, CancellationToken cancellationToken = default)
        {
            await Awaitable.MainThreadAsync();
            cancellationToken.ThrowIfCancellationRequested();
            DeleteCount++;
            CloudError fault = Faults.Next();
            if (fault != null)
            {
                return CloudResult.Failure(fault);
            }

            if (!_items.ContainsKey(key))
            {
                return CloudResult.Success();
            }

            if (writeLock != null && !LockMatches(key, writeLock))
            {
                return CloudResult.Failure(CloudErrorKind.Conflict, $"The write lock for '{key}' does not match.");
            }

            _items.Remove(key);
            return CloudResult.Success();
        }

        public async Awaitable<CloudResult<IReadOnlyList<CloudSaveKeyInfo>>> ListKeysAsync(CancellationToken cancellationToken = default)
        {
            await Awaitable.MainThreadAsync();
            cancellationToken.ThrowIfCancellationRequested();
            ListCount++;
            CloudError fault = Faults.Next();
            if (fault != null)
            {
                return CloudResult<IReadOnlyList<CloudSaveKeyInfo>>.Failure(fault);
            }

            List<CloudSaveKeyInfo> infos = new();
            foreach (KeyValuePair<string, Stored> pair in _items)
            {
                infos.Add(new CloudSaveKeyInfo(pair.Key, pair.Value.Lock.ToString(CultureInfo.InvariantCulture), pair.Value.ModifiedUtc));
            }

            return CloudResult<IReadOnlyList<CloudSaveKeyInfo>>.Success(infos);
        }

        private bool LockMatches(string key, string writeLock)
        {
            return _items.TryGetValue(key, out Stored stored)
                   && string.Equals(stored.Lock.ToString(CultureInfo.InvariantCulture), writeLock, StringComparison.Ordinal);
        }
    }
}
