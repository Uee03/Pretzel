using System;
using UnityEngine;

namespace Pretzel.Pooling
{
    /// <summary>
    /// What code needs from a set of per-prefab pools. <see cref="PoolRegistry"/> is the implementation. Releasing is done with
    /// <see cref="PoolRegistry.Despawn"/> or <see cref="PooledObject.Release"/>, which need no registry instance.
    /// </summary>
    public interface IPoolRegistry : IDisposable
    {
        IComponentPool<T> GetPool<T>(T prefab) where T : Component;

        T Spawn<T>(T prefab) where T : Component;

        T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component;

        GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null);

        void Prewarm<T>(T prefab, int count) where T : Component;
    }
}
