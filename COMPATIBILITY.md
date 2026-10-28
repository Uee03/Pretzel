# Compatibility and release gates

## Support versus evidence

All first-party packages declare Unity `6000.0` as their minimum and carry version `0.2.0`, the first tagged
release (`v0.2.0`). The consumer host is `TestPretzel`; validation uses the modified checkout, not published Git
dependencies. Do not run a second Editor against an open or locked project.

| Gate | Scope | Evidence / required follow-up |
|---|---|---|
| Unity 6000.6.5f1, Windows Editor | Seventeen first-party packages, EditMode suites of thirteen packages, DataIO samples | Local validation; results below |
| Scene loading, PlayMode | `SceneLoader` pipeline, policies, groups, preload, cancellation, failure cleanup and fallback with generated test scenes | 17 PlayMode tests passed twice in a clean host, 6000.6.5f1 (2026-10-27) |
| Package samples | Every `Samples~` folder imported into one host | Compiled with zero C# errors and warnings (integration samples are skipped by their define constraints) |
| Generated examples | Plain, binary and AES use cases and the profile service and startup | Separate compile gate; generation tests also inspect source |
| UI Toolkit theme template | `PretzelTheme.tss` imported as a theme | Imported without errors |
| Unity 6000.0 minimum | Clean consumer tests and generated examples | Not executed. World-space helpers compile only on 6000.2 or newer by design |
| Player persistence | Save, quit, relaunch, load; previous-release saves | Required per shipping platform; not proven by EditMode |
| IL2CPP / stripping, Windows x64 | Generated AES startup and profile service; plain, binary (async) and encrypted round trips | One local smoke player, 6000.6.5f1, Managed Stripping High, passed (2026-10-24). Audio, scenes, pooling and the console were not part of it |
| IL2CPP on other platforms, consoles, mobile | Actual player and backend | Not certified |
| Runtime behaviour in a player | Scene transitions in a player, audio mixing, safe area on notched devices, world-space panels, console input | Manual checks; scene loading is covered in the Editor by PlayMode tests only |
| R3, UniTask, LitMotion adapters | Third-party packages present | Optional assemblies are absent from clean hosts unless those packages are installed. `Pretzel.Integrations.R3` compiled against R3 1.3.0 on 6000.6.5f1 (2026-10-27); R3's own Editor tree view does not compile on 6000.6 (obsolete `TreeView` API) |
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
dependencies and imports the DataIO usage samples. `-UseInstalledPackages` uses the matching packages already in
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

### Local results for v0.2.0

- Unity 6000.6.5f1 / Windows, clean host with seventeen embedded packages: **269 of 269 EditMode tests passed, zero
  skipped**, across core, extensions, tween, api, data, events, pooling, scenes, ui, audio, preferences, devconsole and
  setup.
- Every package sample compiled in one host with zero C# errors and warnings; the theme template imported cleanly.
- The generated DataIO examples (AES startup) compiled with zero C# errors and warnings.
- `Update-ModuleCatalog.ps1 -Check` passed with sixteen modules.
- Unity 6000.0, Linux, player, IL2CPP for the new modules, and visual and interaction checks remain unrun.
