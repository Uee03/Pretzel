---
description: "Unity C# conventions for games, tools, and reusable packages in either 2D or 3D. Preserve nearby style and verify installed API versions."
applyTo: "**/*.cs"
---

# Unity C# conventions

- Support 2D, 3D, Editor tooling, and reusable UPM packages equally. Do not assume a render
  pipeline, physics dimension, scene layout, network stack, or deployment platform.
- Use the Unity project's supported C# language and API compatibility level, not the latest
  desktop .NET SDK. For packages, also respect the declared minimum Unity version.
- Prefer explicit local types over `var`, including for built-in, apparent, and other types.
  Use braces for every control-flow block, including single-statement blocks.
- Use `async`/`await` for genuinely asynchronous I/O when supported by the installed Unity API;
  preserve Unity main-thread requirements and use `Async` suffixes for asynchronous methods.
- Choose string comparison semantics explicitly. Never compare strings with `==`/`!=` or `ToLower`/`ToUpper`;
  use `string.Equals`/`StartsWith`/`Contains` with a `StringComparison`, and give string-keyed dictionaries,
  sets and LINQ lookups a `StringComparer`. Use ordinal, case-sensitive comparison for paths, code tokens and
  identifiers whose semantics are case-sensitive; use `OrdinalIgnoreCase` for text people type. Do not change
  existing path semantics during style cleanup.
- Expose `IReadOnlyList<T>`/`IReadOnlyDictionary<TKey, TValue>` from public properties and methods, and mutate
  through explicit methods. Prefer classes; a struct needs a `// Value type: <reason>` line above it.
- Name private and static fields `_camelCase` and constant fields `PascalCase`. Keep existing serialized field
  names (they are save-file and asset keys); rename only with `[FormerlySerializedAs]` or a data migration.
- Keep one public top-level type per file, named to match the file. Preserve public APIs when
  splitting declarations; do not change serialized identities or behavior.
- Preserve nearby namespaces, braces, field naming, and member ordering. Where already used,
  retain `FIELDS`, `PROPERTIES`, `UNITY METHODS`, and `DO METHODS` regions and `DoOn*` lifecycle
  helpers; do not impose them on an unrelated project or add empty regions.
- Keep MonoBehaviours and views as adapters; put testable rules in ordinary C# types. Use
  interfaces at substitution boundaries, not mechanically for every class. Inject ordinary
  C# dependencies through constructors; initialize components through an explicit composition root.
- Preserve serialized field names, types, enum values, asset GUIDs, and public package APIs.
  Use an appropriate migration when changing serialization. Never edit generated source.
- Use serializable classes for identity and mutable shared state; small structs for genuine
  value semantics. Avoid large struct copying and boxing. Inspect `[SerializeReference]` support
  before using polymorphic fields; Unity does not serialize arbitrary interfaces by default.
- Reuse existing factories, pools, observables, helpers, and Editor utilities before introducing
  alternatives. R3, ZLinq, UniTask, LitMotion, and NuGetForUnity are preferred when appropriate
  and installed, not mandatory dependencies for every feature or package.
- Verify user-added dependencies using manifests, lockfiles, NuGet configuration, asmdefs, and
  installed source. Do not install, update, or remove Unity's project-template defaults.
- Match asynchronous work, subscriptions, tweens, and pooled callbacks to their owner lifetime.
  Propagate cancellation, dispose owned subscriptions, handle exceptions, and reset pooled state.
  Do not block on `.Result`/`.Wait()`, use `async void` beyond required event boundaries, or
  start unobserved fire-and-forget work. Keep Unity object access on the main thread.
- Resolve stable references outside measured hot paths. Do not repeatedly call scene searches,
  `GetComponent`, or `Camera.main` in frame loops when caching is valid. Avoid allocating LINQ,
  captured delegates, and formatting in hot loops; measure before replacing clear cold-path code.
- Do not treat coroutines as threads, assume async APIs are allocation-free, or claim a speedup
  without a representative player-build measurement.
- Keep `UnityEditor` code in Editor-only assemblies. Implement Undo, serialized-property editing,
  and prefab override support when tools modify assets or objects. Preserve `.meta` files.
- Use verified APIs for the installed Unity and package versions. Do not invent graph-editor,
  Addressables, netcode, R3, or LitMotion calls, or hand-edit graph/scene YAML as a shortcut.
- Write comments only for non-obvious intent or constraints, in one short line. Address the user
  as an experienced software engineer; explain trade-offs rather than introductory C# concepts.
- Before finishing any C# change, run the checks in
  [pretzel-code-standards](../skills/pretzel-code-standards/SKILL.md) and fix every error.
