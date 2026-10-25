---
name: unity-scriptable-objects
description: "Design Unity ScriptableObjects for configuration, Flyweight data, strategies, event channels, and runtime sets. Use for authored assets, mutable-state isolation, serialization, or lifecycle issues in 2D/3D projects and reusable packages."
---

# Unity ScriptableObjects

## Workflow

1. Classify the asset as configuration, shared definition, pluggable behavior, event channel,
   or explicitly managed runtime state. Identify which consumers share it and which may mutate it.
2. Prefer authored, read-only definitions plus separately owned runtime state. When cloning an
   asset with `Instantiate`, check nested references: cloning does not promise a deep copy of
   every referenced Unity asset. Destroy owned runtime instances on teardown.
3. Separate authoring DTOs from domain values where serializer constraints differ. Preserve
   serialized field names, enum values, and GUID references; use explicit migrations when needed.
4. If using event channels/runtime sets, define registration, unregistration, reset, and session
   ownership. Domain-reload-disabled play sessions and additive scenes must not retain stale state.
5. Keep persistence separate. Changing a ScriptableObject in a player is not saving it to disk;
   authored assets are not a player save format. Editor mutations can persist unintentionally.
6. For packages, avoid required game-specific asset paths or Resources lookups. Provide contracts
   and optional samples rather than depending on the author's scene/asset hierarchy.
7. Test two consumers for isolation, repeated play sessions, asset serialization, and unload/reset.

## DO NOT

- Store per-player health, inventory, or cooldown in a shared config asset without deliberate ownership.
- Treat ScriptableObject as immutable by default, a dependency injection container, or a universal Singleton.
- Allocate runtime resources from lifecycle callbacks without checking Editor/import behavior.
- Save assets or perform gameplay side effects from `OnValidate`; keep validation local and safe.

## Sources

- [ScriptableObject API](https://docs.unity3d.com/ScriptReference/ScriptableObject.html)
- [ScriptableObject manual](https://docs.unity3d.com/Manual/class-ScriptableObject.html)
- [Flyweight](../unity-design-patterns/reference/flyweight.md)
- [Serialization](https://docs.unity3d.com/Manual/script-serialization.html)
