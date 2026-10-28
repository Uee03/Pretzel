---
name: pretzel-code-standards
description: "Enforce the NET-PROJECT-SETUP coding standard (adapted for Unity C# 9) on every Pretzel package change: explicit types, braces, explicit string comparison, read-only public collections, classes by default, one type per file, naming, Async suffix. Use before finishing any C# edit, when reviewing code, or when asked to check or fix code style and naming."
---

# Pretzel code standards

Source of truth: `Claude/AgenticAssisstance/NET-PROJECT-SETUP.md` section 1.6, applied to Unity packages through
[unity-csharp.instructions.md](../../instructions/unity-csharp.instructions.md) and the root `.editorconfig`.
Unity constraints win where they conflict: C# 9, block namespaces, serialized names, `.meta` GUIDs.

## Always run before finishing a C# change

```powershell
# Line rules over all packages (fast, no build). Exit code 1 on any error.
.\.github\skills\pretzel-code-standards\scripts\Test-CodeStandards.ps1
# Type-aware rules (string comparisons, comparers, ambiguous usings). Needs the .NET SDK, the Unity Editor and an
# imported TestPretzel (its Library). Pass the Editor folder if it differs from the default.
dotnet run --project .github\skills\pretzel-code-standards\tools\StandardsAnalyzer -c Release -- . "D:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor"
```

Both must report 0 errors. Fix the code, never the check, unless the check is wrong; then fix the check and say so.
`-Staged` limits the line rules to staged files (the pre-commit hook in `.githooks/` uses it). `-ShowReview` lists
items that need judgment. Unity compilation and tests still decide correctness; see unity-testing.

## Rules

| Rule                    | Do                                                                                                                                                                                           | Checked by              |
| ----------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------- |
| explicit-type           | `List<Enemy> enemies = new();`, never `var`                                                                                                                                                  | script, `.editorconfig` |
| braces                  | Braces on every `if`/`else`/`for`/`foreach`/`while`/`using`/`lock` body                                                                                                                      | script, `.editorconfig` |
| string-comparison       | `string.Equals(a, b, StringComparison.X)`; `StartsWith`/`EndsWith`/`Contains`/`IndexOf`/`Compare` with a `StringComparison`. No `==`/`!=` between strings, no `ToLower`/`ToUpper` to compare | script, analyzer        |
| string-comparer         | `new Dictionary<string, T>(StringComparer.X)`, `HashSet<string>(StringComparer.X)`; LINQ `Contains`/`Distinct` over strings with a comparer                                                  | script, analyzer        |
| readonly-collection     | Public properties and methods expose `IReadOnlyList<T>`/`IReadOnlyDictionary<TKey, TValue>`; mutate through methods (`AddTransform`, `SetHeader`, `AddEntry`)                                | script                  |
| one-type-per-file       | One public top-level type per file, file named after it (`Type.Variant.cs.txt` allowed for templates)                                                                                        | script                  |
| struct                  | Classes by default. A struct needs `// Value type: <reason>` on the line above (value equality, or a measured allocation-free hot path)                                                      | script                  |
| async-suffix            | Methods returning `Task`/`ValueTask`/`Awaitable`/`UniTask` end in `Async`, except Unity messages                                                                                             | script                  |
| field-name / const-name | Private and static fields `_camelCase`; constant fields `PascalCase` (no `UPPER_CASE`)                                                                                                       | script, `.editorconfig` |
| logging                 | `Debug.Log*`, never `Console.Write*`                                                                                                                                                         | script                  |
| linq (review)           | LINQ where it reads better; avoid it in runtime hot paths (`Update`, per-frame, per-message)                                                                                                 | script `-ShowReview`    |

Choosing a comparison:

- `Ordinal`: code tokens, asset paths and file names from Unity, scene names, UPM package names and versions,
  property paths, secrets and tokens, GUID-like ids that are always the same case.
- `OrdinalIgnoreCase`: anything a person types or edits (catalog ids, preset names, console commands, HTTP header
  names, URI schemes, file extensions, hex fingerprints).
- Never culture-sensitive comparisons for identifiers.

## Documented exceptions

- **Serialized field names stay as they are.** Public camelCase fields on `[Serializable]` classes, `ScriptableObject`s,
  `MonoBehaviour`s and JSON DTOs are the stored keys of save files, settings, catalogs and scenes. Renaming them breaks
  existing data. Change one only with `[FormerlySerializedAs]` (Unity) or a data migration (JSON), never during cleanup.
- Private nested helper classes may use public PascalCase fields.
- The integrations package keeps usings inside the namespace (R3/UniTask name lookup); see `.editorconfig`.
- Templates (`*.cs.txt`) generate user code and follow the same rules.

## Not applicable to Unity packages

Sections of NET-PROJECT-SETUP about `Directory.Build.props`, `Microsoft.Extensions.*` DI/logging/configuration,
xUnit/FluentAssertions, `.slnx`, publish folders and UI icon rules apply to standalone .NET apps only. Packages use
Unity Test Framework (NUnit), `Debug.Log`, asmdefs and manual composition roots (see AGENTS.md).

## When adding a rule

Add it to `Test-CodeStandards.ps1` if a line pattern can find it reliably, otherwise to the analyzer
(`tools/StandardsAnalyzer/Program.cs`). Run both over all packages and fix every finding in the same change.
