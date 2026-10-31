# Bullets And Impacts

A turret that fires pooled bullets, and bullets that spawn pooled impact effects: two pools feeding each other through
an interface, the way a real shooter wires them.

| File | What it shows |
|------|---------------|
| `Turret.cs` | The composition root: builds templates, owns one `PoolRegistry`, prewarms, caps live bullets, shows pool counts, disposes in `OnDestroy`. |
| `Bullet.cs` | `IPoolable` bullet with a swept raycast (a fast bullet cannot tunnel), a lifetime guard through `ReleaseAfter`, and an owner reference cleared on return. |
| `ImpactFlash.cs` | A short effect that returns itself after its duration with `PooledObject.ReleaseAfter`. |
| `IEffectSpawner.cs` | What gameplay needs from effects. Bullets depend on this interface, not on pooling. |
| `PooledEffectSpawner.cs` | The pooled implementation on `PoolRegistry`. Swap it for a fake in tests. |

## Try it

1. Add `Turret` to an empty GameObject and press Play. It builds a ground, a camera and light if the scene has none,
   and fires 25 shots per second at random ground points.
2. Watch the counts: bullets and impacts show active, idle and created totals. They stop growing after a moment, which
   is the point of the pool.
3. Replace the templates with real prefabs: any prefab with a `Bullet` or `ImpactFlash` works, and `PoolRegistry`
   creates the pool on first use.

What to take from it:

- Use `PoolRegistry` when many prefabs share one spawner, and `ComponentPool<T>` when one prefab is hot (the turret
  keeps the bullet pool and calls `Get` directly to skip the dictionary lookup).
- Cap what can be live (`Max Bullets`); a pool that can grow without bound is a leak that takes longer to notice.
- A delayed `ReleaseAfter` is safe: it is ignored when the object was released or taken again in the meantime, so a
  bullet that hit the ground does not release its next life.
- Gameplay depends on a small interface (`IEffectSpawner`), so the pooling strategy can change in one place.
