using System;
using Pretzel.Core;
using Pretzel.Pooling;
using Unity.Netcode;
using UnityEngine;

namespace Pretzel.Multiplayer
{
    /// <summary>
    /// Spawns and despawns a network prefab from a <see cref="ComponentPool{T}"/> instead of instantiating and destroying it.
    /// Register it once per prefab (before the NetworkManager starts, or any time before the first spawn); handlers survive NetworkManager restarts.
    /// The server spawns with <c>NetworkObject.InstantiateAndSpawn</c> and removes with <c>Despawn(true)</c>, clients follow through NGO's spawn and destroy messages;
    /// on both sides NGO calls this handler, so every instance is recycled.
    /// </summary>
    /// <remarks>
    /// What is reset when an instance returns to the pool: parent and scale, <c>SpawnWithObservers</c>, <c>DontDestroyWithOwner</c>, <c>AutoObjectParentSync</c>,
    /// <c>CheckObjectVisibility</c>, then every <see cref="IPoolable.OnReturnedToPool"/> runs. NGO itself clears spawn state, observers and ownership flags on despawn.
    /// NetworkVariable values are not reset by NGO: set initial values on the server in <c>OnNetworkSpawn</c> or <see cref="IPoolable.OnTakenFromPool"/> of the owning behaviour.
    /// </remarks>
    public sealed class PooledNetworkPrefabHandler : INetworkPrefabInstanceHandler, IDisposable
    {
        private readonly NetworkObject _prefab;
        private readonly ComponentPool<NetworkObject> _pool;
        private readonly Vector3 _prefabScale;
        private readonly bool _spawnWithObservers;
        private readonly bool _dontDestroyWithOwner;
        private readonly bool _autoObjectParentSync;
        private NetworkManager _registeredWith;
        private bool _disposed;

        public NetworkObject Prefab => _prefab;

        public int CountActive => _pool.CountActive;

        public int CountInactive => _pool.CountInactive;

        public PooledNetworkPrefabHandler(NetworkObject prefab, int defaultCapacity = 16, int maxSize = 128)
        {
            _prefab = Guard.NotDestroyed(prefab, nameof(prefab));
            _prefabScale = prefab.transform.localScale;
            _spawnWithObservers = prefab.SpawnWithObservers;
            _dontDestroyWithOwner = prefab.DontDestroyWithOwner;
            _autoObjectParentSync = prefab.AutoObjectParentSync;
            // Instances stay at the scene root: NGO rejects parenting a spawned NetworkObject under a transform without a NetworkObject.
            _pool = new ComponentPool<NetworkObject>(prefab, null, defaultCapacity, maxSize);
        }

        /// <summary>Registers this handler for the prefab. Returns false when the prefab already has a handler.</summary>
        public bool Register(NetworkManager networkManager)
        {
            Guard.NotDestroyed(networkManager, nameof(networkManager));
            ThrowIfDisposed();
            if (!networkManager.PrefabHandler.AddHandler(_prefab, this))
            {
                return false;
            }

            _registeredWith = networkManager;
            return true;
        }

        /// <summary>Creates idle instances so the first spawns do not instantiate. Spread large counts over several frames yourself.</summary>
        public void Prewarm(int count)
        {
            ThrowIfDisposed();
            _pool.Prewarm(count);
        }

        public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            if (_disposed)
            {
                return null;
            }

            return _pool.Get(position, rotation);
        }

        public void Destroy(NetworkObject networkObject)
        {
            if (!networkObject)
            {
                return;
            }

            PooledObject marker = networkObject.GetComponent<PooledObject>();
            if (marker == null || !marker.IsInUse)
            {
                return;
            }

            ResetNetworkObject(networkObject);
            _pool.Release(networkObject);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_registeredWith != null && _prefab != null)
            {
                _registeredWith.PrefabHandler.RemoveHandler(_prefab);
            }

            _registeredWith = null;
            _pool.Dispose();
        }

        private void ResetNetworkObject(NetworkObject networkObject)
        {
            networkObject.CheckObjectVisibility = null;
            networkObject.SpawnWithObservers = _spawnWithObservers;
            networkObject.DontDestroyWithOwner = _dontDestroyWithOwner;
            networkObject.AutoObjectParentSync = _autoObjectParentSync;
            networkObject.transform.localScale = _prefabScale;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PooledNetworkPrefabHandler));
            }
        }
    }
}
