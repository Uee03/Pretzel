# Compatibility and release gates

## Support versus evidence

All first-party packages declare Unity `6000.0` as their minimum and currently carry version `0.1.0`.
The default consumer host is `TestPretzel`; validation must use the modified checkout, not its
published Git dependencies. Do not run a second Editor against an open/locked project.

| Gate | Scope | Evidence / required follow-up |
|---|---|---|
| Unity 6000.6.5f1, Windows Editor | Eleven first-party packages, DataIO samples, EditMode suite | Local validation; results below |
| Generated examples | Plain/binary/AES use cases and profile service/startup | Separate compile gate; generation tests also inspect source |
| Unity 6000.0.0f1 minimum | Clean consumer tests and generated examples | CI configured; not locally executed |
| Linux Editor | GameCI matrix | Workflow must run successfully with configured license/image |
| Player persistence | Save/quit/relaunch/load, previous-release saves | Required per shipping platform; not proven by EditMode |
| IL2CPP / stripping, Windows x64 | Generated AES startup + profile service, token `PretzelDataSetup`, sample profile; plain, binary (async) and encrypted round trips | One local smoke player, 6000.6.5f1, Managed Stripping High: built and all round trips passed (2026-10-24) |
| IL2CPP on other platforms / consoles / mobile | Actual player/backend | Not certified |
| R3 / UniTask / LitMotion active adapters | Third-party packages present | Optional assemblies absent from clean-host validation unless explicitly installed |
| Performance/size | Representative target-player payloads | No binary speed/size or encryption performance claim |

The workflow is configuration, not evidence of a completed GitHub run. Unity Docker image
availability and licensing must be confirmed in the repository's runner environment.

## Reproduce a clean local run

Use the matching installed Editor and a **new** disposable destination:

```powershell
.\.github\scripts\New-UnityValidationHost.ps1 `
    -Destination C:\Temp\PretzelValidation `
    -UnityVersion 6000.6.5f1

$process = Start-Process `
    -FilePath "D:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe" `
    -ArgumentList @("-batchmode", "-nographics", "-projectPath", "C:\Temp\PretzelValidation",
        "-runTests", "-testPlatform", "EditMode",
        "-testResults", "C:\Temp\pretzel-results.xml",
        "-logFile", "C:\Temp\pretzel-tests.log") -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Unity failed: $($process.ExitCode)" }
[xml]$results = Get-Content C:\Temp\pretzel-results.xml
if ([int]$results.'test-run'.total -lt 1 -or
    [int]$results.'test-run'.failed -gt 0 -or
    $results.'test-run'.result -notlike "Passed*") {
    throw "No passing NUnit run was produced."
}
```

The script copies all first-party packages as embedded packages, includes their test assemblies,
pins test/Newtonsoft/UGUI dependencies and imports DataIO usage samples.
`-UseInstalledPackages` instead uses the matching packages already in `TestPretzel/Library/PackageCache`
for offline local validation; it fails explicitly if an expected cache entry is missing/ambiguous.
The script never overwrites or deletes a destination.

For generated-code compilation, create a **second** host with `-IncludeGeneratedExamples`.
It includes the independent examples, profile service, encryption demo/Inspector and AES startup.
The CI repeats AES example compilation on both Editors and plain/binary startup on the current Editor.
Some generation tests intentionally skip in that host
to avoid duplicate classes; the separate clean-host run must have no such skips.
Use `-SetupFormat PlainText` or `-SetupFormat Binary` with `-IncludeGeneratedExamples` for those startup
variants. They also have source-level generation tests.

Do not pass `-quit` to the Unity test runner. Inspect XML totals/failures and compiler errors, not
only process exit status. `dotnet build` is not a Unity import/player gate.

## GitHub Actions prerequisites

The [workflow](.github/workflows/unity-validation.yml) uses GameCI's Unity test runner v4 with
checkout-only packages, clean/example hosts, Unity 6000.0.0f1 and 6000.6.5f1.
Configure the appropriate repository secrets before enabling it:

- `UNITY_LICENSE`, plus `UNITY_EMAIL` and `UNITY_PASSWORD` as required for the license type; or
- `UNITY_SERIAL`, `UNITY_EMAIL`, `UNITY_PASSWORD` for a supported serial-based license.

Follow [GameCI's licensing instructions](https://game.ci/docs/github/activation/) and your Unity
license terms. Never commit a license, account credentials or exported AES keys.
The workflow explicitly fails when neither license/serial is configured, and uploads result artifacts.
Fork PRs without secrets will need an authorized maintainer-run validation; do not expose secrets
through `pull_request_target` or run untrusted PR code with privileged credentials.

## Data/save compatibility

| Existing data | Current behavior |
|---|---|
| Plain JSON | Supported, subject to registered schema migrations |
| AES `PRTZ` version 1, mode 1 | Wire format retained; current or retained historical key required |
| Binary `PZBN` version 1 | Supported; uses JSON token semantics/Double numbers |
| Newer unsupported schemas/format versions | Fail explicitly rather than guessing or overwriting |

Automated tests cover binary round-trips/malformed data, an independent AES-v1 fixture,
authentication/history, schema callbacks,
backup/rollback recovery and Editor generation/delivery validation.
They do not replace a previous shipped-player upgrade exercise.

## Release policy

1. Keep dependency direction, serialized enum identities, existing `.meta` GUIDs and documented
   save formats stable within a compatible release. One public type per matching C# file.
2. Pin consumer Git dependencies to a release tag, not a moving branch. Tag only after release gates
   pass; no release/tag is created automatically by validation.
3. Before the next release, select an explicit pre-1.0 breaking-change version/tag for RSA API
   removal and BSON/gzip writer removal. Do not label these changes as a compatible patch.
   The current working manifests remain `0.1.0` until that release decision.
4. Document API removals and conversion steps before consumers upgrade. Preserve original saves and
   keys; migration must write separately and verify. Never silently reinterpret old numeric modes.
5. Run both clean-host CI variants at minimum/current Editors. Review skipped tests and logs.
6. Test shipped-player save/quit/relaunch, upgrades from the previous supported release,
   key delivery without Editor files, and actual backend/stripping/platform settings.
7. Retain AES keys needed by supported saves. A new release does not require key rotation.
8. If a gate cannot run, record the exact missing Editor/license/platform and do not advertise that
   target as certified.

### Local results for this change

- Unity 6000.6.5f1 / Windows: **119 of 119 EditMode tests passed, zero skipped**, with eleven
  first-party packages embedded and DataIO usage samples imported.
- Generated AES, plain JSON and binary startup variants, independent examples, profile service and
  encryption demo/Inspector compiled in an isolated consumer host with zero C# errors/warnings.
- The CI-style AES example host also ran the suite: **106 passed, 13 intentionally skipped, zero
  failed**. Those generation tests skip to avoid duplicate existing classes; Unity reports the root
  as `Skipped:Ignored` with exit code 0. Only generated-consumer jobs accept that root result;
  clean-host jobs require a fully passing, unskipped run.
- Host-preparation script executed for clean and generated-code hosts; PowerShell syntax and
  workflow YAML parsing passed. No GitHub Actions run was performed locally.
- Minimum Unity 6000.0, Linux CI, player/IL2CPP, visual interaction and target-platform persistence
  checks remain unrun.

The historical 101-test run predates AES-only changes and is not evidence for them.
