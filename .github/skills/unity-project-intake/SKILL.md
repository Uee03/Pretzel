---
name: unity-project-intake
description: "Collect and confirm setup choices for a Unity 2D/3D project, Editor tool, or reusable UPM package. Use before scaffolding or adding user-selected R3, ZLinq, UniTask, LitMotion, NuGetForUnity, DI, serialization, or network dependencies; leave Unity template defaults alone."
---

# Unity project and package intake

## Workflow

1. Read existing `AGENTS.md`, relevant folders, and version metadata. A package repository uses
   `package.json` plus a consumer/embedded test host; a project uses ProjectVersion.txt. Do not
   force a project hierarchy into a package or a 2D layout into a 3D game.
2. Ask only unresolved decisions that alter setup: deliverable, Unity/minimum version, targets,
   project/package root, existing architecture, required public APIs, and validation host/commands.
   Rendering/physics dimension matters only if the requested feature uses it.
3. Identify dependencies deliberately added by the user. Inspect UPM manifests/lockfiles, installed
   source and asmdefs, and NuGetForUnity configuration (`packages.config`, `NuGet.config`, configured
   package location). Do not inventory Unity template packages just to recommend replacements.
4. Confirm which of R3, ZLinq, UniTask, LitMotion, NuGetForUnity, serialization extensions, and other
   integrations are required, optional, already installed, or absent. Check supported Unity/API
   profiles, transitive dependencies, license requirements, and IL2CPP/AOT implications.
5. Use [installation and verification](./reference/user-packages.md) for explicit package choices.
   Prefer existing versions; pin approved additions to a released version/tag or commit. Never
   install all preferred packages by default or update existing versions without agreement.
6. Preserve existing manual DI/network wiring. Ask before selecting VContainer/Zenject or a network
   stack. For standalone packages, make dependencies public only if part of the agreed contract.
7. Summarize decisions, exclusions, file changes, package sources/versions, and verification gates.
   Stop for explicit confirmation before scaffolding, dependency edits, or installation commands.
8. After confirmation, implement only that checklist. Compile/import in the matching Editor and
   test in the declared host; report unavailable gates. Do not create separate planning Markdown.

## DO NOT

- Reconfigure Unity's project-template-installed packages or assume latest C#/.NET compatibility.
- Assume R3 exists because NuGetForUnity is installed, or a Unity integration exists because its
  core NuGet package exists. Validate both dependencies and assembly visibility.
- Pick 2D, URP, NGO, a DI container, or sample scenes for a type-agnostic package without approval.
- Add auto-generated boilerplate, project settings, `.vscode` tasks, or publishing scripts outside the scope.

## Sources

- [Unity package layout](https://docs.unity3d.com/Manual/cus-layout.html)
- [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity)
- [Package boundaries](../unity-clean-architecture/reference/package-boundaries.md)
