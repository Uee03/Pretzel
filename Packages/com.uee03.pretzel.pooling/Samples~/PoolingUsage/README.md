# Pooling Usage

| File | What it shows |
|------|---------------|
| `ProjectileSpawner.cs` | A `ComponentPool<T>` owned by a component: prewarmed in `Awake`, disposed in `OnDestroy`, positioned with `Get(position, rotation)`. |
| `Projectile.cs` | `IPoolable` reset hooks and `PooledObject.ReleaseAfter`, so the object returns itself. |

## Try it

1. Create a small prefab (for example a sphere) and add `Projectile`.
2. Add `ProjectileSpawner` to an empty GameObject and assign the prefab.
3. Press Play. Projectiles fly forward and return to the pool after two seconds. The pool's idle instances sit under
   the spawner in the Hierarchy.

Rules:

- Reset everything a previous use could have changed in `OnTakenFromPool`, and stop coroutines, tweens and
  subscriptions in `OnReturnedToPool`.
- Never `Destroy` a pooled object; release it. `PoolRegistry.Despawn` releases or destroys, whichever applies.
- Leave `collectionCheck` on while developing. It throws on a double release.
- `PoolRegistry` keeps one pool per prefab when many prefabs share a spawner.
