using System.Collections.Generic;
using System.Threading;
using Pretzel.Core;
using UnityEngine;

namespace Pretzel.Data
{
    /// <summary>
    /// Thin static facade over one replaceable <see cref="IDataStore"/>, for the simple case.
    /// The default store follows <see cref="DataIOConfig"/> (set up in Pretzel/Data Format), or saves JSON under
    /// <c>Application.persistentDataPath/SaveData</c> when there is no config.
    /// </summary>
    public static class DataIO
    {
        private static volatile IDataStore _default;

        /// <summary>
        /// Forgets the default store, so the next call builds a new one from <see cref="DataIOConfig"/>.
        /// Runs automatically when Play mode starts, which keeps a disabled domain reload from reusing the last session's store.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() => _default = null;

        /// <summary>The store behind every method on this class. Replace it to change location, serializer or options.</summary>
        public static IDataStore Default
        {
            get => _default ??= CreateConfiguredStore();
            set => _default = Guard.NotNull(value, nameof(value));
        }

        private static IDataStore CreateConfiguredStore()
        {
            DataIOConfig config = DataIOConfig.Load();
            return config ? config.CreateStore() : new DataStore(DataRoots.PersistentData("SaveData"));
        }

        public static void Save<T>(DataKey key, T value) => Default.Save(key, value);

        public static LoadResult<T> Load<T>(DataKey key) => Default.Load<T>(key);

        public static bool TryLoad<T>(DataKey key, out T value) => Default.TryLoad(key, out value);

        public static LoadResult<T> LoadInto<T>(DataKey key, T target) where T : class => Default.LoadInto(key, target);

        public static bool Exists(DataKey key) => Default.Exists(key);

        public static bool Delete(DataKey key) => Default.Delete(key);

        public static IReadOnlyList<DataKey> List(string folder = null, int? slot = null) => Default.List(folder, slot);

        public static IReadOnlyList<int> ListSlots(string folder = null) => Default.ListSlots(folder);

        public static Awaitable SaveAsync<T>(DataKey key, T value, CancellationToken cancellationToken = default)
        {
            return Default.SaveAsync(key, value, cancellationToken);
        }

        public static Awaitable<LoadResult<T>> LoadAsync<T>(DataKey key, CancellationToken cancellationToken = default)
        {
            return Default.LoadAsync<T>(key, cancellationToken);
        }

        public static Awaitable<LoadResult<T>> LoadIntoAsync<T>(DataKey key, T target, CancellationToken cancellationToken = default) where T : class
        {
            return Default.LoadIntoAsync(key, target, cancellationToken);
        }

        public static Awaitable<bool> ExistsAsync(DataKey key, CancellationToken cancellationToken = default)
        {
            return Default.ExistsAsync(key, cancellationToken);
        }

        public static Awaitable<bool> DeleteAsync(DataKey key, CancellationToken cancellationToken = default)
        {
            return Default.DeleteAsync(key, cancellationToken);
        }
    }
}
