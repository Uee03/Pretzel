# Enemy Waves

A wave spawner on a keyed `PoolFactory<EnemyKind, WaveEnemy>`: the pattern for enemies, pickups and anything that is
created and destroyed in bulk.

| File | What it shows |
|------|---------------|
| `WaveSpawner.cs` | Factory setup in `Awake` (prewarm per kind), a cap on live enemies, spawning by key, a return callback that keeps the live list correct, and pool counts for tuning. Builds primitive templates in code when no entries are assigned. |
| `WaveEnemy.cs` | `IPoolable` reset hooks, `PooledObject.Release()` and a callback to the owner that is cleared on return. |
| `EnemyKind.cs` | The enum used as the key. |
| `PoolMetricsHud.cs` | Reads `CountActive`, `CountInactive` and `CountAll` from each pool. |

## Try it

1. Add `WaveSpawner` and `PoolMetricsHud` to an empty GameObject at the origin. Press Play.
2. Waves of walkers approach the centre. A defender hits the nearest one; the HUD counts defeated and escaped.
3. Watch **created** per kind. When it stops growing, the pool has reached its peak. Set the prewarm counts (or
   assign your own prefabs under **Enemies > Entries**) to that number, so there are no instantiations during play.

What to take from it:

- Size the prewarm for the peak and the `maxSize` for what you are willing to keep; a cap on live enemies (`Max Alive`)
  stops a wave from growing the pool without bound.
- Anything that varies per spawn (speed, health) is set after `Spawn`; anything that must be clean is reset in
  `OnTakenFromPool`.
- A pooled object tells its owner through a callback in `OnReturnedToPool` and clears it, so there are no stale references
  and no event subscriptions to leak. Take care not to change a list you are iterating when a release removes an entry.
- `Dispose` the factory in `OnDestroy`; it destroys idle and spawned instances under its root.
