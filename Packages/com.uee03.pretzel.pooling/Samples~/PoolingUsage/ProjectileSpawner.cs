using Pretzel.Pooling;
using UnityEngine;

namespace Pretzel.Samples.Pooling
{
    /// <summary>Owns a keyed factory configured in the Inspector, and fires bullets with a rocket every fifth shot.</summary>
    public sealed class ProjectileSpawner : MonoBehaviour
    {
        [Tooltip("One entry per ProjectileKind: the prefab and how many to prewarm.")]
        [SerializeField] private PoolFactory<ProjectileKind, Projectile> _projectiles = new();
        [SerializeField, Min(0.02f)] private float _interval = 0.1f;

        private float _next;
        private int _shots;

        private void Awake()
        {
            // At the scene root, so spawned projectiles do not move with the spawner.
            _projectiles.Initialize();
        }

        private void Update()
        {
            if (Time.time < _next)
            {
                return;
            }

            _next = Time.time + _interval;
            ProjectileKind kind = ++_shots % 5 == 0 ? ProjectileKind.Rocket : ProjectileKind.Bullet;
            Quaternion spread = Quaternion.Euler(0f, Random.Range(-20f, 20f), 0f);
            if (!_projectiles.TrySpawn(kind, transform.position, transform.rotation * spread, out _))
            {
                Debug.LogWarning($"No prefab for {kind}. Add an entry to Projectiles.", this);
                enabled = false;
            }
        }

        private void OnDestroy()
        {
            _projectiles.Dispose();
        }
    }
}
