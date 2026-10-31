# Compatibility and release gates

## Support versus evidence

All first-party packages declare Unity `6000.0` as their minimum and carry version `0.2.0`, the first tagged
release (`v0.2.0`). The consumer host is `TestPretzel`; validation uses the modified checkout, not published Git
dependencies. Do not run a second Editor against an open or locked project.

| Gate | Scope | Evidence / required follow-up |
|---|---|---|
| Unity 6000.6.5f1, Windows Editor | Eighteen first-party packages, EditMode suites of fifteen packages, DataIO samples | Local validation; results below |
| Cloud and Multiplayer packages | Unity Gaming Services adapters (Authentication 3.8.0, Cloud Save 3.4.2, Cloud Code 2.10.4, Remote Config 4.2.5) and Netcode for GameObjects 2.13.3 + Multiplayer Services 2.4.1 | Compile and fake-backed tests (2026-10-29), plus 4 live PlayMode smoke tests in `TestPretzel/Assets/_Project/Tests/LiveServices` (category `LiveServices`: anonymous sign-in, Cloud Save round trip, Cloud Code missing-module failure, Relay session host/find/ready/start/leave) passed against a linked project (2026-10-31). Remote Config, account linking, two real devices, multi-platform and performance were not run |
| Scene loading, PlayMode | `SceneLoader` pipeline, policies, groups, preload, cancellation, failure cleanup and fallback with generated test scenes | 17 PlayMode tests passed twice in a clean host, 6000.6.5f1 (2026-10-27) |
| Package samples | Every `Samples~` folder imported into one host | Compiled with zero C# errors and warnings (integration samples are skipped by their define constraints) |
| Generated examples | Plain, binary and AES use cases and the profile service and startup | Separate compile gate; generation tests also inspect source |
| Unity 6000.0.85f1, Windows Editor | The same clean host as 6000.6 below, with registry package versions for 6000.0 (Test Framework 1.4.5, UGUI 2.0.0) | 705 of 705 EditMode and 27 of 27 PlayMode tests passed, zero compiler errors or warnings (2026-10-31) |
| Player persistence | Save, quit, relaunch, load; previous-release saves | Required per shipping platform; not proven by EditMode |
| IL2CPP / stripping, Windows x64 | Generated AES startup and profile service; plain, binary (async) and encrypted round trips | One local smoke player, 6000.6.5f1, Managed Stripping High, passed (2026-10-24). Audio, scenes, pooling and the console were not part of it |
| IL2CPP on other platforms, consoles, mobile | Actual player and backend | Not certified |
| Runtime behaviour in a player | Scene transitions in a player, audio mixing, safe area on notched devices, world-space panels, console input | Manual checks; scene loading is covered in the Editor by PlayMode tests only |
| R3, UniTask, LitMotion adapters | Third-party packages present | Optional assemblies are absent from clean hosts unless those packages are installed. `Pretzel.Integrations.R3` and `Pretzel.Integrations.R3.Events` compiled against R3 1.3.0 and tested on 6000.0.85f1 and 6000.6.5f1 (2026-10-31); `Pretzel.Integrations.UniTask` compiled against UniTask 2.5.10 on both. R3's and UniTask's own Editor tree views do not compile on 6000.6 (obsolete `TreeView` API), so a 6000.6 host embeds them without their `Editor` folders; 6000.0 compiles them as they are |
| Performance and size | Representative target-player payloads | No speed, size or allocation claim |

## Reproduce a clean local run

Use the matching installed Editor and a **new** destination:

```powershell
.\.github\scripts\New-UnityValidationHost.ps1 `
    -Destination C:\Temp\PretzelValidation `
    -UnityVersion 6000.6.5f1 `
    -UseInstalledPackages

$process = Start-Process `
    -FilePath "D:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe" `
    -ArgumentList @("-batchmode", "-nographics", "-projectPath", "C:\Temp\PretzelValidation",
        "-runTests", "-testPlatform", "EditMode",
        "-testResults", "C:\Temp\pretzel-results.xml",
        "-logFile", "C:\Temp\pretzel-tests.log") -PassThru -Wait
if ($process.ExitCode -ne 0) { throw "Unity failed: $($process.ExitCode)" }
[xml]$results = Get-Content C:\Temp\pretzel-results.xml
if ([int]$results.'test-run'.total -lt 1 -or
    [int]$results.'test-run'.failed -gt 0 -or
    $results.'test-run'.result -notlike "Passed*") {
    throw "No passing NUnit run was produced."
}
```

The script copies every first-party package as an embedded, testable package, pins the test, Newtonsoft and UGUI
dependencies and imports the DataIO usage samples. For the 6000.0 gate, omit `-UseInstalledPackages` and pass
`-UnityVersion 6000.0.85f1`: the defaults are the 6000.0 versions. `-UseInstalledPackages` uses the matching packages already in
`TestPretzel/Library/PackageCache` for offline runs and fails if an expected cache entry is missing or ambiguous.
The script never overwrites or deletes a destination.

To compile all samples, create a second host, copy every `Packages/*/Samples~/<Name>` folder into its `Assets`, delete
the copied `.meta` files and run Unity with `-batchmode -quit`. Check the log for `error CS` and `warning CS`.

For generated-code compilation, create another host with `-IncludeGeneratedExamples` (and `-SetupFormat PlainText`
or `Binary` for those startup variants). Some generation tests intentionally skip in that host to avoid duplicate
classes; the clean-host run must have no skips.

Do not pass `-quit` to the Unity test runner. Inspect XML totals, failures and compiler errors, not only the exit code.

Run the PlayMode suite in the same host with `-testPlatform PlayMode`. The scenes tests create their scenes under
`Assets/PretzelSceneLoaderTests`, add them to the build scene list, and remove both afterwards.
`dotnet build` is not a Unity import or player gate.

## Module catalog

The Setup window's Addons tab reads `Packages/com.uee03.pretzel.setup/Editor/Catalog/modules.json`, generated from the
module manifests. Before a release run:

```powershell
.\.github\scripts\Update-ModuleCatalog.ps1 -Check
```

`ModuleCatalogTests` also fails when an installed module's dependencies differ from the catalog.

## Data and save compatibility

| Existing data | Current behavior |
|---|---|
| Plain JSON | Supported, subject to registered schema migrations |
| AES `PRTZ` version 1, mode 1 | Wire format retained; current or retained historical key required |
| Binary `PZBN` version 1 | Supported; uses JSON token semantics and Double numbers |
| Newer unsupported schemas or format versions | Fail explicitly rather than guessing or overwriting |

Automated tests cover binary round trips and malformed data, an independent AES-v1 fixture, authentication and key
history, schema callbacks, backup and rollback recovery, save slots, preferences and Editor generation and delivery
validation. They do not replace a shipped-player upgrade exercise.

## Release policy

1. Keep dependency direction, serialized enum values, `.meta` GUIDs and documented save formats stable within a
   compatible release. One public type per matching C# file.
2. Pin consumer Git dependencies to a release tag, not a moving branch. Tag only after the gates below pass.
3. Before 1.0, a minor version may break APIs; record removals and conversion steps in the package CHANGELOGs.
   Preserve original saves and keys; migrations write separately and verify. Never reinterpret old numeric modes.
4. Run the clean-host EditMode suite, the sample compile and `Update-ModuleCatalog.ps1 -Check`. Review skipped tests
   and logs.
5. Test shipped-player save, quit, relaunch and load, upgrades from the previous release, key delivery without Editor
   files, and the actual backend, stripping level and platform settings.
6. Retain AES keys needed by supported saves. A new release does not require key rotation.
7. If a gate cannot run, record the missing Editor, platform or device and do not advertise that target as certified.
8. Pretzel targets Unity 6000.0 and newer only. When an API exists only from a later release (or must be avoided from a
   later release), guard it with the version define, for example `#if UNITY_6000_2_OR_NEWER`, and keep the code
   compiling on 6000.0. Today the only guarded code is the world-space UI helpers (`UNITY_6000_2_OR_NEWER`); the whole
   repository compiles without warnings on both 6000.0.85f1 and 6000.6.5f1. Validate on the oldest and the newest
   installed Editor before a release.

### Local results after 0.2.0 (unreleased changes, 2026-10-31)

- Unity 6000.0.85f1 and 6000.6.5f1 / Windows, clean hosts with every package, all 32 samples and the Unity Gaming
  Services, Netcode, Input System, R3, UniTask and VContainer packages installed: **705 of 705 EditMode and 27 of 27
  PlayMode tests passed on each Editor, zero skipped, zero compiler errors or warnings**.

### Local results after 0.2.0 (unreleased changes, 2026-10-29)

- Unity 6000.6.5f1 / Windows, clean host with every package, all 32 samples and the Unity Gaming Services, Netcode and
  Input System packages installed: **688 of 688 EditMode tests passed, zero skipped, zero compiler errors or warnings**.
  Multiplayer additionally passed its PlayMode tests (local loopback host, two players, pooled spawn) in a package host.
- `Update-ModuleCatalog.ps1 -Check` and `Update-SampleCatalog.ps1 -Check` pass (17 modules, 32 samples).

### Local results for v0.2.0

- Unity 6000.6.5f1 / Windows, clean host with seventeen embedded packages: **269 of 269 EditMode tests passed, zero
  skipped**, across core, extensions, tween, api, data, events, pooling, scenes, ui, audio, preferences, devconsole and
  setup.
- Every package sample compiled in one host with zero C# errors and warnings.
- The generated DataIO examples (AES startup) compiled with zero C# errors and warnings.
- `Update-ModuleCatalog.ps1 -Check` passed with sixteen modules.
- Linux, player and IL2CPP builds for the new modules, and visual and interaction checks remain unrun.
