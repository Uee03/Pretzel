using System;
using UnityEngine;

namespace Pretzel.Pooling
{
    /// <summary>
    /// What code needs from a pool of one prefab. <see cref="ComponentPool{T}"/> is the implementation; spawners and gameplay code
    /// can depend on this interface so tests can count spawns with a fake instead of instantiating prefabs.
    /// </summary>
    public interface IComponentPool<T> : IDisposable where T : Component
    {
        int CountActive { get; }

        int CountInactive { get; }

        int CountAll { get; }

        T Prefab { get; }

        T Get();

        T Get(Transform parent);

        T Get(Vector3 position, Quaternion rotation, Transform parent = null);

        /// <summary>Returns an instance to the pool.</summary>
        void Release(T item);

        /// <summary>Creates idle instances until at least <paramref name="count"/> are available.</summary>
        void Prewarm(int count);

        /// <summary>Destroys idle instances. Instances in use stay alive.</summary>
        void Clear();
    }
}
