# AGENTS.md - Pretzel Unity package monorepo

Guidance for the first-party `com.uee03.pretzel.*` UPM packages in this repository. Packages declare
Unity `6000.0` as their minimum; verify the host project and package manifests before relying on a
newer API. `TestPretzel` is the consumer host for Unity import, compilation, and tests.

## Baseline and boundaries

- Read the owning package's `package.json` and asmdefs. Preserve supported Unity versions, C# level,
  targets, dependencies, serialization, and public APIs; never assume a render pipeline or game type.
- Before scaffolding or adding dependencies, complete
  [unity-project-intake](./.github/skills/unity-project-intake/SKILL.md) and obtain confirmation.
  For an existing focused change, read only the relevant nearby implementation and dependency facts.
- Verify package versions and installed APIs before use. Do not add, update, or remove dependencies
  or replace Unity project defaults without approval.
- Prefer manual composition roots unless a DI framework is already selected. Preserve the existing
  network stack; ask before selecting one. Addressables, Input System, and graph skills apply when
  requested or already relevant, not as mandatory installs.
- Package development requires explicit dependencies, Runtime/Editor separation, consumer-facing
  APIs, version compatibility, samples, and tests in a host project. Do not depend on game assemblies.
- Keep pure rules testable, adapters thin, and architecture proportional. Reuse project utilities
  (including LazyJedi where present) rather than cloning them. Patterns solve demonstrated problems.

## Agents

| Agent | Role |
|-------|------|
| [Unity Architect](./.github/agents/unity-architect.agent.md) | Read-only design, trade-offs, and implementation checklist |
| [Unity Developer](./.github/agents/unity-developer.agent.md) | Approved changes with compile/test loops |
| [Unity Performance Auditor](./.github/agents/unity-performance-auditor.agent.md) | Read-only, evidence-ranked performance findings |
| [Unity Reviewer](./.github/agents/unity-reviewer.agent.md) | Read-only correctness, lifecycle, serialization, and API review |
| [Pretzel UITK Editor Tools](./.github/agents/uitk-editor-tools.agent.md) | Pretzel-specific UI Toolkit Editor tools |
| [.NET Reviewer](./.github/agents/dotnet-reviewer.agent.md) | Review standalone .NET code; use Unity-specific review rules for packages |
| [.NET Scaffolder](./.github/agents/dotnet-scaffolder.agent.md) | Only for separately requested standalone .NET tools or services |

Keep guidance model-agnostic. Reviewers distinguish measured facts from hypotheses. Preserve local
customizations and user changes when importing or applying guidance.

## Skills - load only the relevant one

| Skill | Trigger |
|-------|---------|
| [unity-project-intake](./.github/skills/unity-project-intake/SKILL.md) | Project/package setup and user-added dependency decisions |
| [unity-clean-architecture](./.github/skills/unity-clean-architecture/SKILL.md) | SOLID, boundaries, DI, interfaces, serialization, value semantics |
| [unity-design-patterns](./.github/skills/unity-design-patterns/SKILL.md) | Observer, Strategy, Bridge, Singleton, State, Command, Flyweight, Dirty Flag, MVP/MVVM, Chain of Responsibility |
| [unity-object-pooling](./.github/skills/unity-object-pooling/SKILL.md) | Built-in pools, factories, resets, prewarming, resource ownership |
| [unity-optimization](./.github/skills/unity-optimization/SKILL.md) | Profiling, allocations, ZLinq, CPU/GPU/memory budgets |
| [unity-async-reactive](./.github/skills/unity-async-reactive/SKILL.md) | UniTask, R3, LitMotion, coroutines, delegates, Action, Func |
| [unity-scriptable-objects](./.github/skills/unity-scriptable-objects/SKILL.md) | Shared configuration, event channels, runtime state isolation |
| [unity-ui-toolkit](./.github/skills/unity-ui-toolkit/SKILL.md) | Runtime UI architecture: presenters, R3/LitMotion, virtualization, focus, UI lifecycle |
| [unity-editor-tools](./.github/skills/unity-editor-tools/SKILL.md) | EditorWindow, custom inspectors, PropertyDrawers, Undo |
| [uitk-editor-window](./.github/skills/uitk-editor-window/SKILL.md) | UI Toolkit Editor windows, menu items, Settings pages, persisted tool state |
| [uitk-uss-style-system](./.github/skills/uitk-uss-style-system/SKILL.md) | UXML/USS naming (BEM), variables, themes, transitions, editor skins |
| [uitk-inspector-and-drawers](./.github/skills/uitk-inspector-and-drawers/SKILL.md) | `CreateInspectorGUI`, `CreatePropertyGUI`, SerializedObject binding |
| [uitk-custom-controls](./.github/skills/uitk-custom-controls/SKILL.md) | `[UxmlElement]` controls, `BaseField<T>`, events, lifecycle |
| [uitk-data-binding](./.github/skills/uitk-data-binding/SKILL.md) | SerializedObject and runtime data binding, converters, change tracking |
| [uitk-performance-review](./.github/skills/uitk-performance-review/SKILL.md) | UI Toolkit element, style, rendering, binding and leak checklist |
| [unity-addressables](./.github/skills/unity-addressables/SKILL.md) | Asset handles, bundles, catalogs, resource lifetime |
| [unity-scene-loading](./.github/skills/unity-scene-loading/SKILL.md) | Smooth transitions, additive loading, activation and recovery |
| [unity-input-system](./.github/skills/unity-input-system/SKILL.md) | Action maps, rebinding, players, input adapters |
| [unity-netcode](./.github/skills/unity-netcode/SKILL.md) | Multiplayer authority, replication, RPCs, latency and disconnects |
| [unity-shader-graph](./.github/skills/unity-shader-graph/SKILL.md) | Shader Graph, shader properties, pipeline compatibility, GPU cost |
| [unity-vfx-graph](./.github/skills/unity-vfx-graph/SKILL.md) | VFX Graph events, properties, pooling and platform constraints |
| [unity-common-types](./.github/skills/unity-common-types/SKILL.md) | Enums, constants, helpers, shared classes and API design |
| [unity-testing](./.github/skills/unity-testing/SKILL.md) | EditMode/PlayMode, package host tests, player builds |
| [unity-code-review](./.github/skills/unity-code-review/SKILL.md) | Findings-first correctness and compatibility review |
| [unity-adr](./.github/skills/unity-adr/SKILL.md) | Record an approved architecture decision when requested |
| [dotnet-clean-architecture](./.github/skills/dotnet-clean-architecture/SKILL.md) | Apply layering/SOLID ideas selectively; Unity suitability limits are explicit |
| [dotnet-cqrs](./.github/skills/dotnet-cqrs/SKILL.md) | Evaluate CQRS as a pattern only where Unity package domains warrant it |

Path-scoped rules: [unity-csharp.instructions.md](./.github/instructions/unity-csharp.instructions.md).
For separately requested standalone .NET code only, load
[dotnet-conventions.instructions.md](./.github/instructions/dotnet-conventions.instructions.md).

Portable .NET/C# conventions are adapted in
[unity-csharp.instructions.md](./.github/instructions/unity-csharp.instructions.md). Unity-specific
compatibility, lifecycle, serialization, and case-sensitive path semantics take precedence.
ASP.NET, desktop UI, and .NET solution-scaffolding requirements do not apply to UPM packages.
The .NET conventions instruction is on-demand and applies to separately requested .NET code only.

## Verification and asset safety

Use the matching Unity Editor for compile/import checks, EditMode and PlayMode tests, and relevant
player builds. See [unity-testing](./.github/skills/unity-testing/SKILL.md) for batch commands.
Standalone packages need a configured consumer test project. Do not treat `dotnet build` on generated
Unity projects as proof of a valid player build. If Editor/licensing/host access is unavailable,
report exactly which checks remain unrun. Never claim a prompt smoke test or benchmark was executed
without evidence.

Preserve `.meta` GUIDs, serialized identities, and public contracts. Prefer supported Editor APIs
for scene, prefab, and graph changes; do not hand-edit their YAML. Keep disposable work tied to
scene, component, pool lease, or session lifetime. Do not create change-log Markdown or ADR files
unless requested. Official version-matched Unity/package documentation and installed source are
authoritative; community advice is a starting point, not API evidence.