---
name: unity-testing
description: "Verify Unity import/compilation, EditMode and PlayMode tests, serialized assets, player builds, and reusable package consumer hosts. Use for behavior-scoped regressions, async/pooling tests, IL2CPP compatibility, or honest reporting of unavailable Editor gates."
---

# Unity verification

## Workflow

1. Identify the matching Editor, license availability, project/consumer host path, test framework,
   existing test asmdefs, and configured CI gates. Preserve installed package defaults. A standalone
   UPM package needs a host project; do not run Unity against its package folder as if it were a game.
2. Start with the cheapest behavior check that can disconfirm the local hypothesis. Pure rules can
   use existing isolated tests; Unity adapters require appropriate EditMode/PlayMode coverage.
3. Add tests to the neighboring test assembly, not a new framework. Verify test asmdef constraints,
   package test inclusion, and existing discovery settings; never claim success when no tests ran.
4. Test lifecycle risks proportionally: serialization round-trip/migration, pool return/re-rent,
   cancellation during scene unload, listener rebinding, Editor Undo/prefab overrides, network late
   joins/disconnect, and optional package integration absent/present as appropriate.
5. Run matching Unity import/compile and tests using existing tasks/CI or
   [batch commands](./reference/verification-commands.md). Keep test and compile jobs separate.
   Inspect exit status, logs, discovered counts, and XML result failures.
6. Run required representative player builds; IL2CPP/stripping, shader variants, network behavior,
   and NuGet integrations can fail despite Editor success. Use the project's existing build entry point.
7. For performance, use reproducible target-player captures, not an EditMode stopwatch alone.
8. After a local failure, repair the same slice and rerun the same gate. Two failed fixes require
   an investigation handoff. Report skipped/unavailable gates and the precise blocker.

## DO NOT

- Substitute `dotnet build`/`dotnet test` on generated Unity projects for Unity Editor/player validation.
- Add a test host, new packages, generated project files, or change licensing/settings without approval.
- Start a second Editor against a locked project, bypass tests, or treat empty test XML as success.
- Claim visual graph/network/performance correctness from compile-only checks.

## Sources

- [Unity Test Framework - choose installed version](https://docs.unity3d.com/Packages/com.unity.test-framework@latest/)
- [Editor command line](https://docs.unity3d.com/Manual/EditorCommandLineArguments.html)
- [BuildPipeline.BuildPlayer](https://docs.unity3d.com/ScriptReference/BuildPipeline.BuildPlayer.html)
