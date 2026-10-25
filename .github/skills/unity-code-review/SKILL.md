---
name: unity-code-review
description: "Review Unity changes for correctness, SOLID, serialization, lifecycle, async/reactive ownership, pooling, Editor safety, network authority, package API compatibility, and tests. Use for findings-first reviews in 2D/3D projects or reusable packages."
---

# Unity code review

## Workflow

1. Read intended behavior, scoped diff/files, project guidance, supported versions, and relevant
   installed API facts. Preserve user changes; do not widen to a repository cleanup.
2. Trace each modified behavior to the owning method and a nearby call site/test. Prefer an actual
   failure scenario over pattern/style preferences. Explain uncertainties instead of inventing facts.
3. Check Unity lifetimes, equality/null semantics, subscription/tween/async disposal, main-thread
   constraints, pool resets, asset handle ownership, and scene teardown.
4. Check serialized names/types/enums and GUID migration, asmdef/runtime-Editor boundaries,
   minimum-version public APIs, and optional package dependencies absent/present.
5. For Editor tools verify Undo, prefab override, multi-selection, and reload behavior. For network
   code verify authority, input validation, late joins, and disconnect/despawn ordering.
6. Assess SOLID and pattern use only where they affect correctness, substitution, or maintainability.
   Treat performance findings as hypotheses unless captures support them; refer to the Auditor.
7. Identify missing regression tests and unavailable checks. Do not assert a build/test ran merely
   because code looks plausible or the change has a unit-test file.
8. Return findings first, ranked by severity, each with file/line, trigger, impact, evidence, and
   smallest suggested remedy. Then assumptions, test gaps, and a brief summary. Say explicitly
   when no actionable defect was found.

## DO NOT

- Edit files during review, report a preferred pattern as a defect, or demand unnecessary interfaces.
- Use package marketing claims as measured performance evidence or flag every LINQ call equally.
- Claim graph visuals, multiplayer behavior, or IL2CPP compatibility from a text-only review.
- Recommend default package changes or a 2D/3D/pipeline migration outside the requested behavior.

## Sources

- [Architecture](../unity-clean-architecture/SKILL.md)
- [Ownership](../unity-async-reactive/SKILL.md)
- [Tests](../unity-testing/SKILL.md)
- [Unity API reference](https://docs.unity3d.com/ScriptReference/)
