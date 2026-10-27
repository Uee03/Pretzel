# Pretzel Pooling

Prefab pooling on `UnityEngine.Pool`. Depends on Pretzel Core.

```csharp
ComponentPool<Projectile> pool = new(projectilePrefab, parent: transform, defaultCapacity: 32, maxSize: 256);
// Or from Inspector settings: new ComponentPool<Projectile>(projectilePrefab, transform, poolSettings);
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

## PoolFactory: keyed pools from the Inspector

`PoolFactory<TKey, T>` pools several prefabs by key, typically an enum. It is a serializable field, so the prefabs,
prewarm counts and `PoolSettings` are edited in the Inspector of the component that owns it. The owner controls its
lifetime; there is no static instance.

```csharp
public enum ProjectileKind { Bullet, Rocket }

[SerializeField] private PoolFactory<ProjectileKind, Projectile> _projectiles = new();

private void Awake() => _projectiles.Initialize();
private void OnDestroy() => _projectiles.Dispose();

Projectile rocket = _projectiles.Spawn(ProjectileKind.Rocket, muzzle.position, muzzle.rotation);
_projectiles.Release(rocket);
```

- `Initialize` validates every entry first: a missing prefab or a duplicate key throws with the entry index, so a
  broken setup fails at load time instead of on the first spawn.
- Idle instances are grouped under `[Pools]/[Key]` (the root name and grouping are configurable), which keeps the
  Hierarchy readable. Pass a parent to `Initialize` to nest the root.
- `Register(key, prefab, prewarm)` adds pools at runtime, `TrySpawn` avoids exceptions for optional keys, and
  `GetPool(key)` exposes the underlying `ComponentPool<T>` for counts and prewarming.
- Any type Unity can serialize works as the key. Derive a named class (`class ProjectileFactory : PoolFactory<...>`)
  only if you prefer a named type in the Inspector.

For non-GameObject objects use `UnityEngine.Pool.ObjectPool<T>`, `ListPool<T>` and friends directly.

## Sample

`Samples~/PoolingUsage`: an enum-keyed projectile factory with prewarming and self-releasing projectiles.
