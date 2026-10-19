using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Pretzel.Data
{
    /// <summary>
    /// Saves and loads objects by <see cref="DataKey"/>.
    /// Sync methods suit small data. Async methods run file work off the main thread and take a <see cref="CancellationToken"/>.
    /// </summary>
    public interface IDataStore
    {
        ISerializer Serializer { get; }

        /// <summary>Writes atomically. Throws <see cref="System.IO.IOException"/> when the disk write fails.</summary>
        void Save<T>(DataKey key, T value);

        LoadResult<T> Load<T>(DataKey key);

        bool TryLoad<T>(DataKey key, out T value);

        /// <summary>Loads into an existing object. Use this for ScriptableObjects.</summary>
        LoadResult<T> LoadInto<T>(DataKey key, T target) where T : class;

        bool Exists(DataKey key);

        /// <returns>True when a file was deleted.</returns>
        bool Delete(DataKey key);

        /// <summary>Lists the keys in a folder, optionally in one slot. Without a slot, only unslotted keys are returned.</summary>
        IReadOnlyList<DataKey> List(string folder = null, int? slot = null);

        /// <summary>Lists the slot numbers that exist in a folder.</summary>
        IReadOnlyList<int> ListSlots(string folder = null);

        Awaitable SaveAsync<T>(DataKey key, T value, CancellationToken cancellationToken = default);

        Awaitable<LoadResult<T>> LoadAsync<T>(DataKey key, CancellationToken cancellationToken = default);

        Awaitable<LoadResult<T>> LoadIntoAsync<T>(DataKey key, T target, CancellationToken cancellationToken = default) where T : class;

        Awaitable<bool> ExistsAsync(DataKey key, CancellationToken cancellationToken = default);

        Awaitable<bool> DeleteAsync(DataKey key, CancellationToken cancellationToken = default);

        Awaitable<IReadOnlyList<DataKey>> ListAsync(string folder = null, int? slot = null, CancellationToken cancellationToken = default);

        Awaitable<IReadOnlyList<int>> ListSlotsAsync(string folder = null, CancellationToken cancellationToken = default);
    }
}
