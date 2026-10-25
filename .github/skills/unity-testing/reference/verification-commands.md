# Verification commands and evidence

Prefer existing repository tasks/build entry points. These PowerShell examples need a matching
installed Editor and a licensed project/consumer test host. Do not install Unity or create a host
merely to execute them; disclose missing prerequisites. Keep output in the project's ignored directory.

```powershell
$Unity = 'C:\Program Files\Unity\Hub\Editor\<matching-version>\Editor\Unity.exe'
$Project = 'C:\path\to\unity-project-or-approved-package-test-host'
$Output = Join-Path $Project 'Temp\AgentVerification'
New-Item -ItemType Directory -Path $Output -Force | Out-Null

& $Unity -batchmode -quit -projectPath $Project -logFile "$Output\compile.log"
$CompileExit = $LASTEXITCODE

& $Unity -batchmode -projectPath $Project -runTests -testPlatform EditMode `
  -testResults "$Output\editmode.xml" -logFile "$Output\editmode.log"
$EditModeExit = $LASTEXITCODE

& $Unity -batchmode -projectPath $Project -runTests -testPlatform PlayMode `
  -testResults "$Output\playmode.xml" -logFile "$Output\playmode.log"
$PlayModeExit = $LASTEXITCODE
```

The Test Framework controls test-run termination; do not add `-quit` to a test job blindly.
For a focused test add the version-supported `-testFilter <fully-qualified-name-or-filter>`.
Choose actual hardware/graphics support for visual tests; `-nographics` is not a universal option.

Read each result XML and log. A successful exit alone is insufficient if no tests were discovered,
compilation failed, or a result file was not produced. Report executed counts, failures, ignored
tests, and any expected category not run. Compile/import logs should be checked for compiler,
assembly, asset import, and licensing failures. Keep each exit status before launching another job.

For package tests, confirm the host includes the package's test assemblies (for example its existing
manifest `testables` configuration) and resolved dependencies. Adding/altering the host is a separate
approved change. A supported-version matrix should exercise the minimum and current supported versions.

Player builds normally use an existing project-defined method through `-executeMethod`. Do not invent
a method name or assume every project has one. A build report must actually succeed; Editor import
does not prove a player compiles, and a player compiling does not prove runtime behavior.

## Agent smoke checks after installing the bundle

Use separate agent chats with a small fixture/approved test project; never let a smoke prompt mutate
the original game without permission. Actual execution requires an available chat/agent runner.

| Role | Three representative prompts |
|------|------------------------------|
| Architect | Review factory ownership; plan Command undo; design a package with optional R3 |
| Developer | Implement an approved pool reset; add a cancellable transition; integrate an approved package adapter |
| Auditor | Audit factory prewarm; audit coroutine caching; inspect view subscription retention |
| Reviewer | Review serialization migration; review cancellation/despawn; review Editor Undo/asmdef separation |

Check appropriate role tools, stack neutrality, nearby style, verified APIs, ownership, and honest
test reporting. Static frontmatter validation does not prove these prompts or Chat discovery passed.