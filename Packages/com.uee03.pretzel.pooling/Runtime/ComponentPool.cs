using System;
using System.Collections.Generic;
using Pretzel.Core;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Pretzel.Pooling
{
    /// <summary>
    /// Pools instances of one prefab component. Instances are created inactive, so Awake and OnEnable first run when an
    /// instance is first taken. Taken objects are positioned before they are activated, so OnEnable sees the final position.
    /// Every <see cref="IPoolable"/> in the instance's hierarchy is notified. Dispose it to destroy idle instances.
    /// </summary>
    public sealed class ComponentPool<T> : IDisposable where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly ObjectPool<T> _pool;
        private bool _disposed;

        public int CountActive => _pool.CountActive;

        public int CountInactive => _pool.CountInactive;

        public int CountAll => _pool.CountAll;

        public T Prefab => _prefab;

        /// <param name="parent">Idle instances are kept under it. Null keeps them at the scene root.</param>
        /// <param name="collectionCheck">Throws on a double release. Costs a lookup per release, so turn it off once stable.</param>
        /// <param name="maxSize">Instances released beyond this many idle ones are destroyed.</param>
        public ComponentPool(T prefab, Transform parent = null, int defaultCapacity = 10, int maxSize = 1000, bool collectionCheck = true)
        {
            _prefab = Guard.NotDestroyed(prefab, nameof(prefab));
            _parent = parent;
            _pool = new ObjectPool<T>(Create, null, OnRelease, OnDestroyItem, collectionCheck, Mathf.Max(1, defaultCapacity), Mathf.Max(1, maxSize));
        }

        public T Get() => Take(_parent, false, default, default);

        public T Get(Transform parent) => Take(parent, false, default, default);

        public T Get(Vector3 position, Quaternion rotation, Transform parent = null) => Take(parent ? parent : _parent, true, position, rotation);

        /// <summary>Returns an instance. After <see cref="Dispose"/>, the instance is destroyed instead.</summary>
        public void Release(T item)
        {
            Guard.NotDestroyed(item, nameof(item));
            if (_disposed)
            {
                OnDestroyItem(item);
                return;
            }

            _pool.Release(item);
        }

        /// <summary>Creates idle instances until at least <paramref name="count"/> are available.</summary>
        public void Prewarm(int count)
        {
            ThrowIfDisposed();
            int missing = count - _pool.CountInactive;
            if (missing <= 0)
            {
                return;
            }

            List<T> created = ListPool<T>.Get();
            try
            {
                for (int i = 0; i < missing; i++)
                {
                    created.Add(_pool.Get());
                }

                foreach (T item in created)
                {
                    _pool.Release(item);
                }
            }
            finally
            {
                ListPool<T>.Release(created);
            }
        }

        /// <summary>Destroys idle instances. Instances in use stay alive and are destroyed when released.</summary>
        public void Clear() => _pool.Clear();

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _pool.Clear();
            _disposed = true;
        }

        private T Take(Transform parent, bool place, Vector3 position, Quaternion rotation)
        {
            ThrowIfDisposed();
            T item = _pool.Get();
            // An idle instance can be destroyed from outside, for example by a scene unload.
            while (!item)
            {
                item = _pool.Get();
            }

            Transform transform = item.transform;
            if (transform.parent != parent)
            {
                transform.SetParent(parent, false);
            }

            if (place)
            {
                transform.SetPositionAndRotation(position, rotation);
            }

            item.GetComponent<PooledObject>().MarkTaken();
            item.gameObject.SetActive(true);
            Notify(item, true);
            return item;
        }

        private T Create()
        {
            T item = Object.Instantiate(_prefab, InactiveHolder.Transform, false);
            item.gameObject.SetActive(false);
            item.transform.SetParent(_parent, false);
            if (_parent == null)
            {
                SceneManager.MoveGameObjectToScene(item.gameObject, SceneManager.GetActiveScene());
            }

            PooledObject marker = item.GetComponent<PooledObject>();
            if (!marker)
            {
                marker = item.gameObject.AddComponent<PooledObject>();
            }

            marker.Bind(() => Release(item));
            return item;
        }

        private void OnRelease(T item)
        {
            PooledObject marker = item.GetComponent<PooledObject>();
            if (marker.IsInUse)
            {
                Notify(item, false);
            }

            marker.MarkReturned();
            item.gameObject.SetActive(false);
            if (item.transform.parent != _parent)
            {
                item.transform.SetParent(_parent, false);
            }
        }

        private static void OnDestroyItem(T item)
        {
            if (!item)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(item.gameObject);
            }
            else
            {
                Object.DestroyImmediate(item.gameObject);
            }
        }

        private static void Notify(T item, bool taken)
        {
            List<IPoolable> poolables = ListPool<IPoolable>.Get();
            try
            {
                item.GetComponentsInChildren(true, poolables);
                foreach (IPoolable poolable in poolables)
                {
                    try
                    {
                        if (taken)
                        {
                            poolable.OnTakenFromPool();
                        }
                        else
                        {
                            poolable.OnReturnedToPool();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex, item);
                    }
                }
            }
            finally
            {
                ListPool<IPoolable>.Release(poolables);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ComponentPool<T>));
            }
        }
    }
}
