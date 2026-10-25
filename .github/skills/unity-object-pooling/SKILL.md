---
name: unity-object-pooling
description: "Use UnityEngine.Pool ObjectPool, collection pools, factories, prewarming, and lease resets for GameObjects, UI, effects, and reusable packages. Use for pooling design, leaks, double releases, or allocation spikes in either 2D or 3D."
---

# Unity object pooling

## Workflow

1. Locate the existing factory/pool and verify target Unity support. Prefer the built-in
   `UnityEngine.Pool.ObjectPool<T>` or an existing sound wrapper over a new pool implementation.
2. Define creation, checkout, reset/return, overflow destruction, and pool teardown ownership.
   Pool keys must identify compatible instances; a generic base does not prove correct resetting.
3. Reset transform/parent, velocity, animation, health, UI state, references, delegates, R3
   subscriptions, UniTask operations, and LitMotion handles as applicable to the actual object.
   Cancel on return, not merely destruction. Prevent stale callbacks from mutating the next lease.
4. Distinguish retained inactive capacity from active-object limits. `maxSize` constrains retention
   on release, not simultaneous checkouts. Capacity reservation is not instance creation.
5. Prewarm by checking out the desired number of objects simultaneously and then returning them;
   repeated get/release of one instance does not grow the pool. Spread expensive work across frames.
6. Handle collection checks, duplicate releases, scene teardown, and objects destroyed externally.
   Pool clear/dispose does not automatically reclaim every outstanding lease; track active owners.
7. For `ListPool<T>`/collection pools, clear state and guarantee release on exception. Do not let a
   borrowed collection escape its lifetime. Built-in pools are not general-purpose thread-safe pools.
8. If Addressables created the objects, define prefab handle ownership and instance destruction
   consistently. Do not mix ordinary destruction with Addressables instance release indiscriminately.
9. Test prewarm count, reset isolation, return during async work, double release, overflow, and scene
   shutdown. Profile player allocation and retained memory before claiming benefit.

## DO NOT

- Reimplement Unity's existing pool just to demonstrate the pattern.
- Use `OnDisable` as both a return trigger and release callback without a recursion guard.
- Assume pooling is free: retained assets, subscription graphs, and oversizing consume memory.
- Pool GPU VFX, UI, and network objects with identical reset rules without inspecting their lifetimes.

## Sources

- [ObjectPool](https://docs.unity3d.com/ScriptReference/Pool.ObjectPool_1.html)
- [ListPool](https://docs.unity3d.com/ScriptReference/Pool.ListPool_1.html)
- [Async ownership](../unity-async-reactive/SKILL.md)
