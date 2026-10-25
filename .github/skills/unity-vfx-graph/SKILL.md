---
name: unity-vfx-graph
description: "Design Unity VFX Graph systems, exposed properties, events, attribute payloads, property binders, bounds, effect pooling, and GPU budgets. Use for effects integration, visibility, platform support, or reusable VFX packages in 2D or 3D."
---

# Unity VFX Graph

## Workflow

1. Confirm VFX Graph relevance, version, pipeline, compute/graphics support, target platform,
   intended space, and budget. Do not infer support from a 2D/3D project label or install by default.
2. Inspect the existing asset's exposed property/event names and typed payload contract. Use cached
   property IDs where supported and check existence/types using verified VisualEffect APIs.
3. Define spawn/update/output behavior, bounds/culling, capacity, lifetime, and deterministic vs
   visual-only requirements. Do not equate GPU particle simulation with authoritative gameplay physics.
4. Clarify ownership of VisualEffect instances, event attributes, binders, and authored assets.
   Follow documented disposal for owned native-backed payloads and avoid allocating them per frame.
5. For pooling, define stop/clear/reinitialize semantics, reset properties and spawn parameters, and
   avoid stale triggers after return. Test actual graph behavior; disabling alone may not reset it.
6. Editing graph nodes requires a verified supported Editor/tool surface. Otherwise return precise
   manual graph steps, with only code integrations implemented; preserve graph identities/GUIDs.
7. Profile GPU particles/overdraw, bounds, warmup, capacity, and synchronization. Validate effect
   appearance, culling, repeated playback, pooling resets, target player support, and teardown memory.
8. Package assets must declare dependencies and avoid game-scene references; optional integration
   assemblies must still compile when the effect package is absent.

## DO NOT

- Invent VFX Graph editor APIs, hand-rewrite graph files, or assume all platforms support compute effects.
- Increase capacity or bounds blindly to hide missing particles; diagnose culling/spawn behavior.
- Reuse pooling reset rules without testing graph state, or retain disposed event attributes.

## Sources

- [VFX Graph documentation - choose installed version](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@latest/)
- [VisualEffect API](https://docs.unity3d.com/ScriptReference/VFX.VisualEffect.html)
- [Pool ownership](../unity-object-pooling/SKILL.md)
