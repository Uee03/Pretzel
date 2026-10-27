using System;
using System.Collections.Generic;
using Pretzel.Core;
using UnityEngine;

namespace Pretzel.Pooling
{
    /// <summary>
    /// One <see cref="ComponentPool{T}"/> per prefab, created on first spawn. Use it when many prefabs share one spawner.
    /// Release spawned objects with <see cref="Despawn"/> or <see cref="PooledObject.Release"/>.
    /// </summary>
    public sealed class PoolRegistry : IDisposable
    {
        private readonly Dictionary<Component, IDisposable> _pools = new();
        private readonly Transform _root;
        private readonly int _defaultCapacity;
        private readonly int _maxSize;

        /// <param name="root">Idle instances of every pool are kept under it.</param>
        public PoolRegistry(Transform root = null, int defaultCapacity = 10, int maxSize = 1000)
        {
            _root = root;
            _defaultCapacity = defaultCapacity;
            _maxSize = maxSize;
        }

        public ComponentPool<T> GetPool<T>(T prefab) where T : Component
        {
            Guard.NotDestroyed(prefab, nameof(prefab));
            if (_pools.TryGetValue(prefab, out IDisposable existing))
            {
                return (ComponentPool<T>)existing;
            }

            ComponentPool<T> pool = new(prefab, _root, _defaultCapacity, _maxSize);
            _pools.Add(prefab, pool);
            return pool;
        }

        public T Spawn<T>(T prefab) where T : Component => GetPool(prefab).Get();

        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
        {
            return GetPool(prefab).Get(position, rotation, parent);
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            Guard.NotDestroyed(prefab, nameof(prefab));
            return Spawn(prefab.transform, position, rotation, parent).gameObject;
        }

        /// <summary>Returns a spawned object to its pool, or destroys it when it did not come from a pool.</summary>
        public static void Despawn(GameObject instance)
        {
            if (!instance)
            {
                return;
            }

            if (instance.TryGetComponent(out PooledObject pooled))
            {
                pooled.Release();
            }
            else
            {
                UnityEngine.Object.Destroy(instance);
            }
        }

        public void Prewarm<T>(T prefab, int count) where T : Component => GetPool(prefab).Prewarm(count);

        public void Dispose()
        {
            foreach (IDisposable pool in _pools.Values)
            {
                pool.Dispose();
            }

            _pools.Clear();
        }
    }
}
