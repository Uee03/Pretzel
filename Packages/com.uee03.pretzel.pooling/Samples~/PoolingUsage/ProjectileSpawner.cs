using Pretzel.Pooling;
using UnityEngine;

namespace Pretzel.Samples.Pooling
{
    /// <summary>Owns a pool for its lifetime, prewarms it, and fires on an interval.</summary>
    public sealed class ProjectileSpawner : MonoBehaviour
    {
        [SerializeField] private Projectile _prefab;
        [SerializeField, Min(0.02f)] private float _interval = 0.1f;
        [SerializeField, Min(0)] private int _prewarm = 20;

        private ComponentPool<Projectile> _pool;
        private float _next;

        private void Awake()
        {
            if (!_prefab)
            {
                Debug.LogError("Assign a Projectile prefab.", this);
                enabled = false;
                return;
            }

            _pool = new ComponentPool<Projectile>(_prefab, transform, defaultCapacity: _prewarm, maxSize: 200);
            _pool.Prewarm(_prewarm);
        }

        private void Update()
        {
            if (Time.time < _next)
            {
                return;
            }

            _next = Time.time + _interval;
            Quaternion spread = Quaternion.Euler(0f, Random.Range(-20f, 20f), 0f);
            _pool.Get(transform.position, transform.rotation * spread);
        }

        private void OnDestroy()
        {
            _pool?.Dispose();
        }
    }
}
