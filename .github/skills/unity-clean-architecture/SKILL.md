---
name: unity-clean-architecture
description: "Design Unity SOLID boundaries, interfaces, services, dependency injection, asmdefs, serializable classes, and value versus reference types. Use for 2D/3D gameplay, Editor tools, or reusable UPM package architecture."
---

# Unity clean architecture

## Workflow

1. Identify the feature's rules, Unity adapters, lifetime, public contract, and smallest testable
   slice. Preserve the existing layout; do not impose an enterprise solution structure on a game.
2. Keep rules in ordinary C# types where practical. Dependencies point toward rules/contracts;
   MonoBehaviours, UI, persistence, networking, and rendering implement outer adapters.
3. Apply SOLID pragmatically: cohesive responsibilities, extension at actual variation points,
   substitutable contracts, small consumer-specific interfaces, and explicit dependencies.
   An interface for every DTO or concrete value type does not improve the design.
4. Use constructor injection for ordinary types. A scene/bootstrap composition root supplies
   serialized references and initializes components explicitly; do not constructor-inject
   MonoBehaviours created by Unity. Prefer manual wiring; use an existing DI container if selected.
5. Inspect Unity serialization: `[SerializeField]` for supported fields, `[Serializable]` for
   embedded data, and `[SerializeReference]` for supported managed polymorphism. Interfaces are
   not ordinary Inspector fields; preserve migration paths and existing subtype selectors.
6. Choose small structs for genuine values and classes for shared mutable identity. Consider
   copies, boxing, equality, default values, and serializer compatibility. A readonly struct can
   be a good domain type without being a supported authoring format; map through a serializable DTO.
7. Define asmdefs only where dependency, platform, or test separation warrants them. Runtime
   must not reference Editor or game-specific assemblies. Avoid cycles and hidden auto-references.
8. For reusable packages, load [package boundaries](./reference/package-boundaries.md). Keep
   R3/UniTask/LitMotion integration optional unless part of the approved public contract.
9. Verify pure-rule tests, Unity import/compile, serialized round-trip, and required player targets.

## DO NOT

- Put rules in giant managers, views, custom inspectors, or service locators.
- Force every project into Domain/Application/Infrastructure folders or add a DI framework by habit.
- Leak game assets, scenes, or concrete package integrations through a supposedly reusable API.
- Assume desktop .NET APIs, records, reflection, or source generators work on the Unity baseline.

## Sources

- [Unity serialization](https://docs.unity3d.com/Manual/script-serialization.html)
- [SerializeReference](https://docs.unity3d.com/ScriptReference/SerializeReference.html)
- [Assembly definitions](https://docs.unity3d.com/Manual/assembly-definition-files.html)
- [ScriptableObject guidance](../unity-scriptable-objects/SKILL.md)
