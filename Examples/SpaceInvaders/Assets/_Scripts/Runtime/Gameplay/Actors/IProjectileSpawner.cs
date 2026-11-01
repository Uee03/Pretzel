using UnityEngine;

namespace SpaceInvaders.Gameplay
{
    /// <summary>What shooters need from the projectile pools, so the player and the invaders never touch a prefab.</summary>
    public interface IProjectileSpawner
    {
        Projectile Spawn(ProjectileKind kind, Vector3 position);

        int CountActive(ProjectileKind kind);
    }
}
