---
name: unity-addressables
description: "Design and implement Unity Addressables asset, prefab, and scene loading with explicit handle ownership, groups, catalogs, remote content, and memory release. Use for leaks, load failures, content updates, or package-safe asset adapters."
---

# Unity Addressables

## Workflow

1. Confirm Addressables is relevant and installed; read its selected version and existing content
   setup. Do not install it merely because the project uses Unity or UniTask.
2. Trace one asset from key/AssetReference through load, consumption, and release. Define a single
   owner for each acquired handle, with explicit reference-sharing rules for multiple consumers.
3. Separate direct asset loads, instantiated prefabs, and loaded scenes. Match release APIs to
   the actual acquisition mode and documented handle tracking options. Do not release a prefab's
   supporting asset handle while live instances still depend on it.
4. Check status and exceptions; define invalid-key, missing-dependency, download, cancellation,
   and retry behavior. Cancelling a UniTask wait does not imply the underlying Addressables
   operation stopped. Complete/observe abandoned operations and release ownership exactly once.
5. For pooling, usually retain the prefab asset for the pool lifetime and explicitly destroy
   instances/retire active leases before releasing it; preserve an existing correct acquisition model.
6. Inspect groups, packing, duplicated dependencies, catalog/profile selection, remote endpoints,
   and content update strategy only as needed. Separate runtime code from Editor content builds.
7. For package APIs, accept an asset-provider contract or explicit AssetReference when that dependency
   is deliberate. Do not assume a consumer's group names, labels, build paths, or remote service.
8. Test success, failed load, owner destruction mid-load, repeated use/release, scene unload, offline
   content, and the built content in a player. Inspect native/managed memory after cycles.

## DO NOT

- Treat a valid handle as proof its load succeeded, or release the same ownership twice.
- Mix Destroy/ReleaseInstance/Release without checking the acquisition contract.
- Assume Editor fast-mode success proves bundles/catalogs work in a built player.
- Hand-edit content groups or upgrade packages outside the approved scope.

## Sources

- [Addressables documentation - choose installed version](https://docs.unity3d.com/Packages/com.unity.addressables@latest/)
- [Async ownership](../unity-async-reactive/SKILL.md)
- [Scene transitions](../unity-scene-loading/SKILL.md)
