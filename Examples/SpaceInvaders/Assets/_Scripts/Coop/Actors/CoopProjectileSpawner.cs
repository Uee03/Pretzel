using Pretzel.Multiplayer;
using SpaceInvaders.Gameplay;
using Unity.Netcode;
using UnityEngine;

namespace SpaceInvaders.Coop
{
    /// <summary>
    /// Spawns bullets and bombs for the host. The prefabs are network prefabs that <see cref="PretzelNetworkBootstrap"/> pools, so every
    /// spawn and despawn is a recycle on every machine. The invader formation uses it through <see cref="IProjectileSpawner"/>, exactly
    /// as it uses the local spawner in the single-player game.
    /// </summary>
    public sealed class CoopProjectileSpawner : MonoBehaviour, IProjectileSpawner
    {
        [SerializeField] private PretzelNetworkBootstrap _bootstrap;
        [SerializeField] private GameObject _bulletPrefab;
        [SerializeField] private GameObject _bombPrefab;

        /// <summary>The slot of the player whose bullet is hitting something right now, or -1. Set around a hit by <see cref="CoopProjectile"/>.</summary>
        public int CurrentShooterSlot { get; set; } = -1;

        // The invaders never use the projectile itself, and only the host spawns; the shooter-aware overload is the one players call.
        public Projectile Spawn(ProjectileKind kind, Vector3 position)
        {
            SpawnFor(kind, position, -1);
            return null;
        }

        public CoopProjectile SpawnFor(ProjectileKind kind, Vector3 position, int ownerSlot)
        {
            NetworkManager manager = _bootstrap != null ? _bootstrap.NetworkManager : null;
            if (manager == null || !manager.IsServer || !manager.IsListening)
            {
                return null;
            }

            NetworkObject spawned = NetworkObject.InstantiateAndSpawn(PrefabFor(kind), manager, NetworkManager.ServerClientId, false, false, false, position, Quaternion.identity);
            if (spawned == null || !spawned.TryGetComponent(out CoopProjectile projectile))
            {
                return null;
            }

            projectile.ServerBind(this, ownerSlot);
            return projectile;
        }

        public int CountActive(ProjectileKind kind)
        {
            GameObject prefab = PrefabFor(kind);
            foreach (PooledNetworkPrefabHandler handler in _bootstrap.PooledHandlers)
            {
                if (handler.Prefab.gameObject == prefab)
                {
                    return handler.CountActive;
                }
            }

            return 0;
        }

        private GameObject PrefabFor(ProjectileKind kind)
        {
            return kind == ProjectileKind.Bullet ? _bulletPrefab : _bombPrefab;
        }
    }
}
