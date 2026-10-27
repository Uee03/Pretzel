# Pretzel Pooling

Prefab pooling on `UnityEngine.Pool`. Depends on Pretzel Core.

```csharp
ComponentPool<Projectile> pool = new(projectilePrefab, parent: transform, defaultCapacity: 32, maxSize: 256);
pool.Prewarm(32);

Projectile shot = pool.Get(muzzle.position, muzzle.rotation);
pool.Release(shot);          // or shot.GetComponent<PooledObject>().Release()
pool.Dispose();              // in OnDestroy of the owner
```

- Instances are created inactive under a hidden holder, so `Awake` and `OnEnable` first run when an instance is first
  taken, and prewarming has no side effects.
- `Get(position, rotation)` positions the object before it is activated, so `OnEnable` sees the final position.
- Every `IPoolable` in the instance's hierarchy gets `OnTakenFromPool` after activation and `OnReturnedToPool` before
  deactivation. Reset state there.
- Each instance has a `PooledObject` that can `Release()` itself or `ReleaseAfter(seconds)`. A delayed release is
  ignored when the object was released or taken again in the meantime.
- `collectionCheck` (on by default) throws on a double release. Releases beyond `maxSize` idle instances are destroyed.
- After `Dispose`, idle instances are destroyed and instances still in use are destroyed when released.

`PoolRegistry` keeps one pool per prefab for spawners that handle many prefabs:
`registry.Spawn(prefab, position, rotation)` and `PoolRegistry.Despawn(gameObject)`.

For non-GameObject objects use `UnityEngine.Pool.ObjectPool<T>`, `ListPool<T>` and friends directly.

## Sample

`Samples~/PoolingUsage`: a prewarmed projectile spawner with self-releasing projectiles.
