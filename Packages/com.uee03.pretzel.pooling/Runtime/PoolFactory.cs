using System;
using System.Collections.Generic;
using Pretzel.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pretzel.Pooling
{
    /// <summary>
    /// Pools several prefabs by key, such as an enum of projectile types, configured in the Inspector. Add it as a field of the
    /// component that owns it, call <see cref="Initialize"/> once and <see cref="Dispose"/> when the owner is destroyed.
    /// Idle instances are grouped under a root object with one child per key, which keeps the Hierarchy readable.
    /// Derive a concrete class only if you want a named type; the generic one serializes as it is.
    /// </summary>
    [Serializable]
    public class PoolFactory<TKey, T> : IDisposable where T : Component
    {
        [SerializeField] private PoolSettings _settings = new();

        [Tooltip("Name of the object that holds every idle instance.")]
        [SerializeField] private string _rootName = "[Pools]";

        [Tooltip("Gives each key its own child under the root.")]
        [SerializeField] private bool _groupByKey = true;

        [SerializeField] private List<PoolFactoryEntry<TKey, T>> _entries = new();

        private readonly Dictionary<TKey, ComponentPool<T>> _pools = new();
        private Transform _root;

        public bool IsInitialized { get; private set; }

        public IReadOnlyCollection<TKey> Keys => _pools.Keys;

        public PoolSettings Settings => _settings;

        /// <summary>Entries used by <see cref="Initialize"/>.</summary>
        public IReadOnlyList<PoolFactoryEntry<TKey, T>> Entries => _entries;

        /// <summary>Adds an entry from code. Only allowed before <see cref="Initialize"/>; use <see cref="Register"/> afterwards.</summary>
        public PoolFactory<TKey, T> AddEntry(TKey key, T prefab, int prewarm = 0)
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("Entries cannot change after Initialize. Use Register to add a pool.");
            }

            _entries.Add(new PoolFactoryEntry<TKey, T>(key, prefab, prewarm));
            return this;
        }

        /// <summary>Validates the entries, creates one pool per key and prewarms them. Later calls do nothing.</summary>
        /// <param name="parent">Parent of the root object. Null keeps it at the root of the active scene.</param>
        /// <exception cref="InvalidOperationException">An entry has no prefab, or two entries share a key.</exception>
        public void Initialize(Transform parent = null)
        {
            if (IsInitialized)
            {
                return;
            }

            Validate();
            _root = new GameObject(string.IsNullOrWhiteSpace(_rootName) ? "[Pools]" : _rootName).transform;
            _root.SetParent(parent, false);
            IsInitialized = true;
            foreach (PoolFactoryEntry<TKey, T> entry in _entries)
            {
                Register(entry.key, entry.prefab, entry.prewarm);
            }
        }

        /// <summary>Adds a pool at runtime, for example for content loaded later.</summary>
        public ComponentPool<T> Register(TKey key, T prefab, int prewarm = 0)
        {
            ThrowIfNotInitialized();
            Guard.NotDestroyed(prefab, nameof(prefab));
            if (_pools.ContainsKey(key))
            {
                throw new InvalidOperationException($"A pool for key '{key}' already exists.");
            }

            Transform parent = _root;
            if (_groupByKey)
            {
                parent = new GameObject($"[{key}]").transform;
                parent.SetParent(_root, false);
            }

            ComponentPool<T> pool = new(prefab, parent, _settings);
            _pools.Add(key, pool);
            if (prewarm > 0)
            {
                pool.Prewarm(prewarm);
            }

            return pool;
        }

        public bool Contains(TKey key) => _pools.ContainsKey(key);

        /// <exception cref="KeyNotFoundException">No pool exists for the key.</exception>
        public ComponentPool<T> GetPool(TKey key)
        {
            ThrowIfNotInitialized();
            if (_pools.TryGetValue(key, out ComponentPool<T> pool))
            {
                return pool;
            }

            throw new KeyNotFoundException($"No pool for key '{key}'. Add an entry for it or call Register.");
        }

        public T Spawn(TKey key) => GetPool(key).Get();

        public T Spawn(TKey key, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            return GetPool(key).Get(position, rotation, parent);
        }

        public bool TrySpawn(TKey key, Vector3 position, Quaternion rotation, out T instance)
        {
            if (IsInitialized && _pools.TryGetValue(key, out ComponentPool<T> pool))
            {
                instance = pool.Get(position, rotation);
                return true;
            }

            instance = null;
            return false;
        }

        /// <summary>Returns an instance to its pool, or destroys it when it did not come from a pool.</summary>
        public void Release(T instance) => PoolRegistry.Despawn(Guard.NotDestroyed(instance, nameof(instance)).gameObject);

        public void Prewarm(TKey key, int count) => GetPool(key).Prewarm(count);

        /// <summary>
        /// Destroys the root object and every instance under it, including spawned ones still parented there.
        /// Instances spawned under another parent are destroyed when they are released.
        /// </summary>
        public void Dispose()
        {
            foreach (ComponentPool<T> pool in _pools.Values)
            {
                pool.Dispose();
            }

            _pools.Clear();
            if (_root)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(_root.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(_root.gameObject);
                }
            }

            _root = null;
            IsInitialized = false;
        }

        private void Validate()
        {
            HashSet<TKey> keys = new();
            for (int i = 0; i < _entries.Count; i++)
            {
                PoolFactoryEntry<TKey, T> entry = _entries[i];
                if (entry == null || !entry.prefab)
                {
                    throw new InvalidOperationException($"Pool entry {i} has no prefab.");
                }

                if (!keys.Add(entry.key))
                {
                    throw new InvalidOperationException($"Pool entry {i} uses the key '{entry.key}' a second time.");
                }
            }
        }

        private void ThrowIfNotInitialized()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Call Initialize before using the factory.");
            }
        }
    }
}
