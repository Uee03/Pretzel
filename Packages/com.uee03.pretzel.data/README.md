# Pretzel Data

Save and load Unity data with readable JSON, binary encoding or authenticated AES encryption.
Plain JSON remains the zero-setup default. **AES is the default and only encryption option.**

Depends on `pretzel.core` and Unity's Newtonsoft JSON package. Editor tools
live in the separate `Pretzel.Data.Editor` assembly and do not require Project Setup.

## Start here

For a small JSON save, no generated code or key is needed:

```csharp
using Pretzel.Data;
using UnityEngine;

// Use your serializable Profile class.
LoadResult<Profile> result = DataIO.Load<Profile>("profile");
switch (result.Status)
{
    case DataStatus.Ok:
        profile = result.Value;
        if (result.RecoveredFromBackup)
        {
            Debug.LogWarning("Profile recovered from backup.");
        }
        break;
    case DataStatus.NotFound:
        profile = new Profile(); // First run; save only when the player chooses to.
        break;
    case DataStatus.Corrupt:
        Debug.LogError($"Profile could not be loaded ({result.ErrorCode}): {result.Error}");
        // Keep the existing file. Offer retry/recovery; do not silently replace it.
        return;
}

// At a checkpoint, after a successful load or an explicitly chosen new game:
DataIO.Save("profile", profile); // Throws if saving fails; do not report success on failure.
```

Do not use `ValueOr(new Profile())` for important saves: it treats a missing save and an unreadable
save alike. Check the result first. Async calls accept a cancellation token; await checkpoint saves
rather than relying on an unfinished save in `OnDestroy` or `OnApplicationQuit`.

### Choose your route

| Need | Route | Keys? |
|---|---|---|
| Editable settings or a first save | Default JSON, or **Generate plain / binary setup** | No |
| Binary inventory/save data | Select Binary, then **Generate plain / binary setup** | No |
| Discourage casual save editing | **Generate AES encryption setup** | One persistent AES key |

Open **Pretzel > Data Format > Setup**. Generate the setup for your route, then call
`ProjectDataIO.Initialize()` once from startup (`Start` or later), before using `DataIO`.
The asset `Assets/Resources/Pretzel/DataIOConfig.asset` holds settings, not secrets. The generated
setup reads it and applies optional code overrides to a copy.

For AES, choose embedded C# or bundled JSON delivery. The setup creates a key only when needed,
stores it in the Editor key file, exports it for the game, and sets the matching key source.
**Back up that key separately. Do not generate a new key each run or each release.**
Keep generated key-bearing files and their metadata out of public version control.

Run **Test save and load** to round-trip temporary diagnostic data using the configured Editor key
source. It does not change player profiles or generate keys. **Open store folder** shows the default
configured save location. Overrides and custom roots can use a different location.
**Check build setup** performs limited delivery checks, not proof of working player startup.

The three independent example buttons generate different use cases: editable JSON settings,
binary inventory, and an AES-protected profile. Each uses its own store/strategy and folder without
changing `DataIO.Default`. Add the generated component to a GameObject, enter Play mode and use its
context menu. The AES example needs the configured key source.

**Add profile service example** supplies an optional slot-oriented save service, profile class,
demo component and [setup guide](Documentation~/DataIOSetup.md). Generated files belong to you;
regeneration asks before replacing a different setup and does not overwrite existing example files.
Previously generated scripts are not upgraded automatically.

## What the formats mean

| Format | Extension | Encoding | Limitations |
|---|---|---|---|
| `PlainText` | `.json` | UTF-8 JSON | Readable and editable; no confidentiality/authentication |
| `Binary` | `.bin` or `.dat` | Versioned .NET `BinaryWriter` / `BinaryReader` token format | Not encrypted; not guaranteed smaller or faster |
| `Encrypted` | `.pretzel` | AES-256-CBC + HMAC-SHA256, random IV per file | Client-held keys are extractable |

Binary and encrypted output may optionally be Base64 text (roughly 33 percent larger).
`BinaryFormatter` is never used.

An AES key used by an offline game can be recovered by a determined player, even with IL2CPP.
Authentication detects edits made without the key; it does not make client data authoritative or
prevent restoring an older valid save. Keep multiplayer authority and genuine secrets on a server.
No server, token service or routine key rotation is required for an ordinary offline save.

## Interchangeable strategies (advanced)

Keep gameplay on `IDataStore`; select encoding at the composition root:

```csharp
IDataFormatStrategy strategy = new BinaryDataStrategy();
// Other choices: new PlainTextStrategy(), new EncryptedDataStrategy(keys).
IDataStore inventory = DataStoreFactory.CreateWithStrategy(
    DataRoots.PersistentData("Inventory"), strategy);
inventory.Save("items", items);
```

Create each store once and retain it. Use separate folders for different encodings. Switching
strategy, extension or key does not convert existing files. The enum factory and settings asset use
these same strategies. `ISerializer` controls object-to-JSON mapping; strategies encode its output.
`NewtonsoftSerializer` includes Unity value-type converters; `JsonUtilitySerializer` supports
Unity's field/ScriptableObject model. Custom formats implement `IDataFormatStrategy`.

`DataKey` contains a validated name, optional slot and folder:
`new DataKey("profile", slot: 2, folder: "Profiles")`. A string converts to a simple key.
`DataRoots` provides persistent data, temporary cache and absolute roots, plus Editor-only project
and user settings roots. `DataIO` is a thin facade over the replaceable `DataIO.Default`.
The `ToJson`/`FromJson` string helpers use a shared `NewtonsoftSerializer` (or one you pass) and
never build the default store.

### Schema upgrades without replacing generated startup

The `configureStore` callback runs before the store snapshots its options. For a profile whose
version 1 payload used `gold`, version 2 can migrate it to `coins`:

```csharp
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pretzel.Data;

[SchemaVersion(2)]
public sealed class Profile
{
    public int coins;
}

// At startup, before the first DataIO call:
ProjectDataIO.Initialize(configureStore: options =>
{
    options.AddMigration<Profile>(new DelegateMigration(1, 2, json =>
    {
        JObject payload = JObject.Parse(json);
        payload["coins"] = payload.Value<int?>("gold") ?? 0;
        payload.Remove("gold");
        return payload.ToString(Formatting.None);
    }));
});
```

The previous release must have saved `[SchemaVersion(1)]` data. Unversioned files start at schema
0 and need a 0-to-1 step too. Register every required step; newer unsupported schemas fail rather
than being downgraded. Save after a successful migrated load to persist the current schema.
The same callback is available on generated `CreateStore`, `DataIOConfig.CreateStore` and factories.

### Load failures and write guarantees

`LoadResult<T>` distinguishes `Ok`, `NotFound` and `Corrupt`. Inspect `Error` and `ErrorCode`:

| `DataError` | Action |
|---|---|
| `InvalidData` | Preserve malformed/truncated data; offer backup/import/retry |
| `MissingKey` | Restore the configured key; do not create a replacement save |
| `AuthenticationFailed` | Wrong key or modified/damaged encrypted data; these cannot be distinguished |
| `UnsupportedFormat` | Use a compatible reader/conversion release |
| `SchemaMismatch` | Register missing migrations or use a compatible game version |
| `IoFailure` | Check permissions, file locks, storage and retry |

A missing/invalid key at store construction throws with setup guidance. Expected read failures
become results; invalid API usage and unexpected programmer exceptions can still throw.
Handle save I/O, access and encryption errors explicitly at your UI/event boundary.

Writes flush a temporary file then attempt `File.Replace`. Where replacement is unavailable, a
recoverable fallback retains the previous primary in `.rollback` until the new primary is installed.
The next store access restores an interrupted fallback if the primary is missing; a present primary
wins over a leftover rollback. `.bak` recovery is enabled by default and reports
`RecoveredFromBackup`. This is **not** a universal atomicity or power-loss durability guarantee.
Back up important saves separately and test your target filesystem.
Operations are locked per store instance, not across stores/processes sharing a path; coordinate
writes through one owner. Within one store, saves and deletes of a key apply in call order: a slower
background `SaveAsync` never replaces data from a later `Save`, `SaveAsync` or `Delete`.

The `.bak` copy is only used when the primary is missing, malformed or fails authentication. A
primary with a newer schema, an unsupported format or a missing key is reported as-is, so an older
backup never replaces newer progress. A primary that cannot be read because of an I/O error loads
from `.bak` but is left on disk.

### IL2CPP and code stripping

Newtonsoft creates save types through reflection. With Managed Stripping Level above Minimal, keep
your save types: mark them `[UnityEngine.Scripting.Preserve]` (type and constructor) or list their
assembly in a `link.xml`. Test a stripped IL2CPP player before release.

## AES key delivery

Use one backed-up key for a typical game's lifetime.

| `KeySource` | Reads | Typical use |
|---|---|---|
| `Provider` | `EncryptionKeys.Provider` only | Embedded setup or your own in-memory provider |
| `BundledJson` | `Resources/Pretzel/GameEncryptionKeys.json` only | Simplest offline delivery |
| `Environment` | `<PREFIX>_AES_KEY` (Base64) only | Tools/managed machines, not ordinary players |
| `Automatic` | Provider, environment, Editor key file, bundled JSON | Development |

The Editor key file `UserSettings/Pretzel/pretzel-keys.json` never ships.
JSON and embedded C# exports do ship; masking is obfuscation, not secrecy.
An existing store keeps its key/configuration; finish saves, shut down and initialize again to
change them. Optional previous-key history remains in advanced controls, not a routine requirement.
See [key delivery](Documentation~/EncryptionKeyDelivery.md) and
[encryption guidance](Documentation~/EncryptionBestPractices.md).

## Compatibility and migration

### Binary wire format

Binary converts JSON/JToken values, not objects directly. Header: ASCII `PZBN`, version byte `1`,
then one tagged value. Tags `0..7`: null, bool, Int64, Double, UTF-8 string, object, array, big integer
(invariant decimal string). Counts are Int32; strings use `BinaryWriter.Write(string)` length
prefixes. Numeric encoding is .NET little-endian. Depth, counts, headers, versions and trailing bytes
are checked. It never instantiates types from file metadata.
Double encoding does not preserve arbitrary decimal precision; use JSON or a project-owned strategy
when that precision matters. Benchmark representative player data before assuming a gain.

### Moving from LazyJedi

| LazyJedi | Pretzel |
|---|---|
| `DataIO.Save(data, "name")` | `DataIO.Save("name", data)` |
| `PathType` argument | A store with the matching `DataRoots.*` |
| Direct `T` from `Load` | Check `LoadResult<T>.Status` before using `Value` |
| `LoadAndOverwrite` | `LoadInto("name", target)` |
| Slot-specific methods | `new DataKey("name", slot: 2)` |
| Default name from the type | Pass an explicit name |
| `PrintAllPaths` | `List()` and `ListSlots()` |
| `SerializableGuid` | Same name in `Pretzel.Data`; validated string and matching equality/hash |

Import `Samples~/DataUsage` from Package Manager for keys, slots, migrations, async saves and AES.
See the repository [compatibility and release gates](https://github.com/Uee03/Pretzel/blob/main/COMPATIBILITY.md)
for validation scope.
