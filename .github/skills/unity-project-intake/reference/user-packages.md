# User-added packages: installation and verification

Use the installed/released documentation for the selected versions. The following are upstream
installation entry points, not commands to run without approval. Pin a released tag/commit for
Git UPM dependencies rather than retaining a floating branch. Do not guess the release tag.

| Package | Approved installation route | Verify |
|---------|-----------------------------|--------|
| NuGetForUnity | Upstream Git UPM URL or its documented OpenUPM registry | Editor extension, NuGet configuration and restore behavior |
| UniTask | Upstream Git UPM URL or documented registry | Cysharp.Threading.Tasks assembly, version, PlayerLoop integrations |
| R3 | Core `R3` via NuGetForUnity plus upstream Unity integration UPM package if Unity helpers are needed | Core and integration versions, asmdef references, Unity lifecycle/time providers |
| ZLinq | Core `ZLinq` via NuGetForUnity plus upstream `ZLinq.Unity` integration if needed | AsValueEnumerable operators, integration assembly, target API compatibility |
| LitMotion | Upstream Git UPM package or documented registry | Core bindings, optional integration defines/asmdefs, selected tween APIs |
| SerializeReference extensions | Existing/upstream selected package only if polymorphic authoring needs it | Inspector subtype selection, runtime serialized type resolution |

## Upstream entry points

```text
NuGetForUnity: https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity
UniTask: https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
R3 Unity: https://github.com/Cysharp/R3.git?path=src/R3.Unity/Assets/R3.Unity
ZLinq Unity: https://github.com/Cysharp/ZLinq.git?path=src/ZLinq.Unity/Assets/ZLinq.Unity
LitMotion: https://github.com/annulusgames/LitMotion.git?path=src/LitMotion/Assets/LitMotion
```

Add an approved `#<tag-or-commit>` to Git URLs using the upstream package's supported release.
Use Unity Package Manager for UPM dependencies and NuGetForUnity's package workflow for NuGet core
libraries. Restore pinned NuGet dependencies through its documented mechanism; do not blindly copy
DLLs or run desktop `dotnet add package` against Unity-generated projects.

## Compatibility checks

- Confirm minimum Unity/runtime profile and each integration's release/assembly references.
  A library being .NET Standard-compatible does not prove its full Unity/IL2CPP behavior.
- Inspect NuGet configuration before assuming package storage paths. Keep feeds authenticated
  outside committed secrets; never add private registry tokens to manifests or examples.
- Avoid duplicate assemblies installed through both UPM and NuGet/manual DLL routes. Confirm asmdef
  visibility, transitive versions, and optional defines before attempting API calls.
- R3/UniTask are not mandatory public dependencies of every package. Put optional integrations in
  separate verified assemblies; test both the absence and presence of those integrations.
- Do not enable ZLinq's drop-in generator by default. Source-generator/compiler compatibility and
  changed overload resolution require explicit approval and a consumer compile check.
- Verify compile/import, relevant EditMode/PlayMode behavior, supported player/IL2CPP targets,
  clean restore, and package-host consumer builds where required.

Sources: [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity),
[UniTask](https://github.com/Cysharp/UniTask), [R3 Unity](https://github.com/Cysharp/R3#unity),
[ZLinq Unity](https://github.com/Cysharp/ZLinq#unity), [LitMotion](https://github.com/annulusgames/LitMotion).