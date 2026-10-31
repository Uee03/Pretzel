using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>
    /// Moves every live bullet from one <see cref="ITickable.Tick"/>: one PlayerLoop call for all bullets instead of
    /// one MonoBehaviour.Update per bullet. Disposing the scope destroys the pool.
    /// </summary>
    public sealed class BulletSystem : ITickable, IDisposable
    {
        private readonly BulletSettings _settings;
        private readonly List<Bullet> _live = new();
        private readonly ObjectPool<Bullet> _pool;
        private readonly Transform _root;

        public BulletSystem(BulletSettings settings)
        {
            _settings = settings;
            _root = new GameObject("Bullets").transform;
            _pool = new ObjectPool<Bullet>(
                Create,
                bullet => bullet.Transform.gameObject.SetActive(true),
                bullet => bullet.Transform.gameObject.SetActive(false),
                defaultCapacity: settings.poolCapacity);
        }

        public int LiveCount => _live.Count;

        public void Fire(Vector3 position, Vector3 direction)
        {
            Bullet bullet = _pool.Get();
            bullet.Transform.localPosition = position;
            bullet.Velocity = direction.normalized * _settings.speed;
            bullet.Age = 0f;
            _live.Add(bullet);
        }

        public void Tick()
        {
            float deltaTime = Time.deltaTime;
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                Bullet bullet = _live[i];
                bullet.Age += deltaTime;
                if (bullet.Age >= _settings.lifetime)
                {
                    // Swap with the last bullet so removal does not shift the list.
                    int last = _live.Count - 1;
                    _live[i] = _live[last];
                    _live.RemoveAt(last);
                    _pool.Release(bullet);
                    continue;
                }

                bullet.Transform.localPosition += bullet.Velocity * deltaTime;
            }
        }

        public void Dispose()
        {
            _live.Clear();
            _pool.Dispose();
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root.gameObject);
            }
        }

        private Bullet Create()
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.Destroy(sphere.GetComponent<Collider>());
            sphere.name = "Bullet";
            sphere.transform.SetParent(_root, false);
            sphere.transform.localScale = Vector3.one * _settings.size;
            return new Bullet(sphere.transform);
        }
    }
}
