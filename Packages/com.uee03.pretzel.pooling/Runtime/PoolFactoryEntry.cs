using System;
using UnityEngine;

namespace Pretzel.Pooling
{
    /// <summary>One prefab of a <see cref="PoolFactory{TKey,T}"/> and the key that spawns it.</summary>
    [Serializable]
    public sealed class PoolFactoryEntry<TKey, T> where T : Component
    {
        public TKey key;
        public T prefab;

        [Tooltip("Idle instances created when the factory initializes.")]
        [Min(0)] public int prewarm;

        public PoolFactoryEntry()
        {
        }

        public PoolFactoryEntry(TKey key, T prefab, int prewarm = 0)
        {
            this.key = key;
            this.prefab = prefab;
            this.prewarm = prewarm;
        }
    }
}
