# Pooling Usage

| File | What it shows |
|------|---------------|
| `ProjectileSpawner.cs` | A `PoolFactory<ProjectileKind, Projectile>` field configured in the Inspector: initialized in `Awake`, disposed in `OnDestroy`, spawning by key with `TrySpawn`. |
| `ProjectileKind.cs` | The enum used as the factory key. |
| `Projectile.cs` | `IPoolable` reset hooks and `PooledObject.ReleaseAfter`, so the object returns itself. |

## Try it

1. Create two small prefabs (for example a sphere and a capsule) and add `Projectile` to both.
2. Add `ProjectileSpawner` to an empty GameObject. Under **Projectiles > Entries**, add one entry per kind with its
   prefab and a prewarm count.
3. Press Play. Bullets fly forward with a rocket every fifth shot, and return to their pools after two seconds. Idle
   instances sit under `[Pools]/[Bullet]` and `[Pools]/[Rocket]` in the Hierarchy.

Rules:

- Reset everything a previous use could have changed in `OnTakenFromPool`, and stop coroutines, tweens and
  subscriptions in `OnReturnedToPool`.
- Never `Destroy` a pooled object; release it. `PoolFactory.Release` and `PoolRegistry.Despawn` release or destroy,
  whichever applies.
- Leave **Collection Check** on while developing. It throws on a double release.
- Use `ComponentPool<T>` directly for a single prefab, and `PoolRegistry` when prefabs are only known at runtime.
