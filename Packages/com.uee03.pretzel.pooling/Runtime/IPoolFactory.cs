using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pretzel.Pooling
{
    /// <summary>
    /// What code needs from a keyed set of pools, such as an enum of projectile kinds. <see cref="PoolFactory{TKey,T}"/> is the
    /// implementation. Depend on this interface in spawners so tests can use a fake.
    /// </summary>
    public interface IPoolFactory<TKey, T> : IDisposable where T : Component
    {
        bool IsInitialized { get; }

        IReadOnlyCollection<TKey> Keys { get; }

        /// <summary>Validates the entries, creates one pool per key and prewarms them. Later calls do nothing.</summary>
        void Initialize(Transform parent = null);

        IComponentPool<T> Register(TKey key, T prefab, int prewarm = 0);

        bool Contains(TKey key);

        /// <exception cref="KeyNotFoundException">No pool exists for the key.</exception>
        IComponentPool<T> GetPool(TKey key);

        T Spawn(TKey key);

        T Spawn(TKey key, Vector3 position, Quaternion rotation, Transform parent = null);

        bool TrySpawn(TKey key, Vector3 position, Quaternion rotation, out T instance);

        /// <summary>Returns an instance to its pool, or destroys it when it did not come from a pool.</summary>
        void Release(T instance);

        void Prewarm(TKey key, int count);
    }
}
