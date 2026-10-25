# Flyweight

Use to share immutable intrinsic data across many entities, such as item definitions, mesh/material
references, or authored ability configuration. Instances retain their own extrinsic state.

- ScriptableObjects are convenient authored definitions; a shared asset is not per-player health,
  cooldown, or runtime inventory. Keep runtime state in owned ordinary objects/components.
- Define the lifetime of shared assets and Addressables handles. Do not release shared resources
  while any consumer still depends on them.
- Measure reduced memory against indirection and lookup costs; test per-instance isolation.

Flyweight shares data; pooling reuses instances. Do not mutate supposedly intrinsic data from a
single consumer or assume ScriptableObject assets are automatically immutable.