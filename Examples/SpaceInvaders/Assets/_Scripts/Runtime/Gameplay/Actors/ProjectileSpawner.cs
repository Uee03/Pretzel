using Pretzel.Pooling;
using UnityEngine;

namespace SpaceInvaders.Gameplay
{
    /// <summary>Owns the keyed projectile pools. The prefabs and prewarm counts are set in the Inspector.</summary>
    public sealed class ProjectileSpawner : MonoBehaviour, IProjectileSpawner
    {
        [SerializeField] private PoolFactory<ProjectileKind, Projectile> _projectiles = new();

        private void Awake()
        {
            _projectiles.Initialize();
        }

        private void OnDestroy()
        {
            _projectiles.Dispose();
        }

        public Projectile Spawn(ProjectileKind kind, Vector3 position)
        {
            return _projectiles.Spawn(kind, position, Quaternion.identity);
        }

        public int CountActive(ProjectileKind kind)
        {
            return _projectiles.GetPool(kind).CountActive;
        }
    }
}
