using Pretzel.Pooling;
using UnityEngine;

namespace Pretzel.Samples.Pooling.Bullets
{
    /// <summary>
    /// Flies straight, checks the path it covers this frame with a raycast (so a fast bullet cannot tunnel through a thin
    /// collider), spawns an impact effect and returns itself. A lifetime guards against bullets that never hit anything.
    /// </summary>
    public sealed class Bullet : MonoBehaviour, IPoolable
    {
        [SerializeField, Min(1f)] private float _speed = 30f;
        [SerializeField, Min(0.1f)] private float _lifetime = 3f;

        private PooledObject _pooled;
        private IEffectSpawner _effects;

        private void Awake()
        {
            _pooled = GetComponent<PooledObject>();
        }

        /// <summary>Called by the shooter right after the bullet is taken from its pool.</summary>
        public void Launch(IEffectSpawner effects)
        {
            _effects = effects;
            _pooled.ReleaseAfter(_lifetime);
        }

        private void Update()
        {
            float step = _speed * Time.deltaTime;
            Vector3 forward = transform.forward;
            if (Physics.Raycast(transform.position, forward, out RaycastHit hit, step))
            {
                _effects?.SpawnImpact(hit.point, hit.normal);
                _pooled.Release();
                return;
            }

            transform.position += forward * step;
        }

        public void OnTakenFromPool()
        {
        }

        public void OnReturnedToPool()
        {
            _effects = null;
        }
    }
}
