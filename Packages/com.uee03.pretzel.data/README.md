# Pretzel Data

Save and load with a pluggable serializer, plus `SerializableGuid`. Depends on `pretzel.core`, `pretzel.extensions` and `com.unity.nuget.newtonsoft-json`. The `Editor` folder adds the Data Format window and the `SerializableGuid` drawer. It is compiled for the Editor only (`Pretzel.Data.Editor`) and does not depend on the Project Setup window.

```csharp
DataIO.Save("profile", profile);

LoadResult<PlayerProfile> result = DataIO.Load<PlayerProfile>("profile");
PlayerProfile loaded = result.ValueOr(new PlayerProfile());

DataKey slot = new("profile", slot: 2, folder: "Profiles");
await DataIO.SaveAsync(slot, profile, token);
```

## Concepts

- `DataKey`: a name, an optional slot and an optional folder. Validated, so a key cannot leave the store root. A `string` converts to a key.
- `DataStore`: the instance that does the work. `new DataStore(root, serializer, extension, options)`. `DataIO` is a thin static facade over `DataIO.Default`, which you can replace.
- `IDataRoot` / `DataRoots`: where files go. `PersistentData`, `TemporaryCache`, `Absolute`, and in the editor `ProjectSettings` and `UserSettings`. Roots resolve on first use.
- `ISerializer`: `NewtonsoftSerializer` by default, `JsonUtilitySerializer` for ScriptableObjects. Converters for `Vector2`, `Vector3`, `Quaternion`, `Color` and `SerializableGuid` are included.
- `LoadResult<T>`: `Ok`, `NotFound` or `Corrupt`. Missing and corrupt files are results, not exceptions or silent defaults.
- `DataStoreOptions`: `PrettyPrint`, `KeepBackup` (on by default, recovers from `.bak`), `SlotPrefix`, `Transforms` (such as `GZipTransform`) and `AddMigration<T>`.
- Writes are atomic: a temp file, then a replace.
- Versioned types use `[SchemaVersion(n)]` and `IMigration` steps.
- `ToJson`, `FromJson` and `FromJsonOverwrite` string helpers use the default serializer.

## Formats

JSON is the default. The other formats are optional providers that run after the serializer, as byte transforms.

| Format | Extension | What it does | Use it for |
|---|---|---|---|
| `PlainText` | `.json` | Readable JSON | Settings, debugging, anything you want to diff |
| `Compressed` | `.json.gz` | GZip | Large saves, cloud sync |
| `Binary` | `.bin` or `.dat` | BSON (binary JSON). Not secret, often not smaller | Discouraging casual editing |
| `Encrypted` | `.pretzel` | AES-256-CBC + HMAC-SHA256, random IV per file. Optional RSA (2048, 3072 or 4096) wraps the AES key | Tamper-proof or private data |

Any format except plain text can also be written as Base64 text (`Base64Transform`, about 33 percent bigger), for pasting or putting in a JSON field.

`DataIO.Default` follows `DataIOConfig`, an asset at `Assets/Resources/Pretzel/DataIOConfig.asset` that the **Pretzel/Data Format** window creates and edits. It holds no secrets. Without it, `DataIO` saves plain JSON. The window has these tabs:

- **Format**: Plain text, Compressed, Binary or Encrypted, each with pros, cons and when to use it. Serializer, save folder, extension, pretty print, backup and Base64 text.
- **Encryption**: AES-256 or RSA public key + AES-256, RSA key size (2048, 3072 or 4096, with 4096 recommended), key generation, and a checklist for what to do with an encrypted JSON.
- **Keys**: store keys as user environment variables, or in the per-project key file `UserSettings/Pretzel/pretzel-keys.json`, which is plain text and must stay out of version control. Environment variables are per user, not per project, so each project gets its own variable prefix. The **Game-specific keys** section exports a game-readable JSON config or generates the existing embedded C# setup. Both export only the current mode's AES key or RSA public key, never the RSA private key.
- **Shipping**: writes `PretzelDataSetup.cs` (environment check, embedded key, or token fetch), a standalone `PretzelEncryptionExample.cs`, and a copy of the key delivery guide into your Assets.
- **Decrypt**: paste plain JSON, or the Base64 text of a Compressed, Binary or Encrypted file, to decode it. It can also encode JSON with the current format, and take other keys without saving them.

Generated keys that are not stored yet survive a script reload, and closing the window asks whether to save them to the key file. In code:

```csharp
EncryptionKeys keys = EncryptionKeys.Resolve("PRETZEL_MYGAME");
DataStore store = DataStoreFactory.Create(DataRoots.PersistentData("SaveData"), DataFormat.Encrypted, keys);
```

`EncryptionKeys.Resolve` takes each key from the first source that has it: `EncryptionKeys.Provider` (your code, for example keys fetched with a token), the environment variables `<PREFIX>_AES_KEY`, `<PREFIX>_RSA_PUBLIC` and `<PREFIX>_RSA_PRIVATE` (Base64), in the Editor `UserSettings/Pretzel/pretzel-keys.json`, then the optional bundled `Assets/Resources/Pretzel/GameEncryptionKeys.json`. The JSON is loaded automatically in both the Editor and player builds; no startup script is needed. A missing config is optional; a malformed config throws rather than silently ignoring it. A wrong key, or an edited file, loads as `Corrupt`.

For offline encrypted saves, generate an AES key, then choose **Keys > Game-specific keys > Save game key config (JSON)**. Alternatively, **Generate embedded C# setup** writes `Assets/Pretzel/Generated/PretzelDataSetup.cs`, using the same generator as Shipping. Choose one method and remove stale generated files when switching. Restart Play mode after exporting; an already-created store keeps its keys. Keep a separate backup of every key needed by existing saves: exporting a new key does not migrate old saves. RSA exports leave the private key unstored, so save that separately through the Editor-only storage options.

Bundling an AES key is a convenience for discouraging casual save editing, not secure secret storage. JSON is plain text; the generated C# uses XOR-masked arrays. IL2CPP compiles the code to native code but does not make embedded keys secret. Exclude generated AES files (including their `.meta` files) from version control; unlike `DataIOConfig.asset`, these exports contain key material.

Two encryption modes:

- **AES**: one secret key encrypts and decrypts. The game needs the key, so it is only as safe as the key's delivery.
- **RSA**: each file gets a random AES key that is wrapped with an RSA public key. The game only needs the public key to write. Reading needs the private key, which must never ship with the game. Use it for data you send to a server.

The IV is random per file and stored in the file. There is no IV to store. Read [Documentation~/EncryptionBestPractices.md](Documentation~/EncryptionBestPractices.md) for the best practices behind this and how to apply them (passphrase keys, key rotation, reading files on a server), and [Documentation~/EncryptionKeyDelivery.md](Documentation~/EncryptionKeyDelivery.md) for where keys can live, how to ship them, and how to fetch them with a token.

Notes: the RSA padding is OAEP with SHA-1, the only OAEP Mono supports. `Binary` uses the BSON types of the bundled Newtonsoft, which are marked obsolete but still work. In a small test the byte counts were JSON 179, GZip 157, BSON 201 and Encrypted 246.

## Moving from LazyJedi

Slot methods are gone. A slot is part of the key. `PathType` is replaced by `IDataRoot`. Load results never throw for a missing file.

| LazyJedi | Pretzel |
|---|---|
| `DataIO.Save(data, "name")` | `DataIO.Save("name", data)` |
| `DataIO.Save(data, "name", prettyPrint: true)` | `DataIO.Default = new DataStore(root, options: new DataStoreOptions { PrettyPrint = true })`, then `Save` |
| `DataIO.Save(data, "name", PathType.X)` | Create a `DataStore` with the matching `DataRoots.*` root |
| `T x = DataIO.Load<T>("name")` | `DataIO.Load<T>("name").ValueOr(fallback)` or `DataIO.TryLoad("name", out T x)` |
| `DataIO.LoadAndOverwrite(obj, "name")` | `DataIO.LoadInto("name", obj)` |
| `DataIO.SaveToSlot(data, 2, "name")` | `DataIO.Save(new DataKey("name", slot: 2), data)` |
| `DataIO.LoadFromSlot<T>(2, "name")` | `DataIO.Load<T>(new DataKey("name", slot: 2))` |
| `DataIO.LoadAndOverwriteFromSlot(obj, 2, "name")` | `DataIO.LoadInto(new DataKey("name", slot: 2), obj)` |
| `DataIO.Delete<T>("name")` | `DataIO.Delete("name")` |
| `DataIO.DeleteFromSlot<T>(2, "name")` | `DataIO.Delete(new DataKey("name", slot: 2))` |
| default name from `typeof(T).Name` | Not used. Always pass a name |
| `DataIO.PrintAllPaths()` | `DataIO.List()` and `DataIO.ListSlots()` |
| `SerializableGuid` | Same name in `Pretzel.Data`. The string constructor validates, and the hash matches `Equals` |

## Sample

`Samples~/DataUsage`: keys, slots, a ScriptableObject, a migration and an async save.

Import **Data Usage** from Package Manager, add `EncryptionUsageExample` to a GameObject, and use its component context menu in Play mode:

1. For offline saves, select **Encrypted + AES** in **Pretzel > Data Format**, generate a key, then export JSON or embedded C# on **Keys** (or supply environment variables). **Save and load configured AES profile** demonstrates `DataIO.Save` / `DataIO.Load` with slots and explicit load-error handling.
2. For client-to-server data, select **Encrypted + RSA**, generate a pair, store the private key in Editor-only storage, and export only the public key. **Write RSA report for server** creates `report.pretzel` in `persistentDataPath/EncryptionExamples/RsaOutbox`. Send that file over HTTPS; `ReadRsaReportOnServer` shows how a trusted server/tool decrypts it using its private key. RSA is not a client-side save/load replacement.
3. Read `CreateAesStore` for explicit stores and JSON/environment/provider sources. `LoadAesKeyAfterSignInAsync` accepts your own authenticated key-service request, installs the returned AES key, and demonstrates async saving/loading with cancellation. Do not call `DataIO` before the key arrives. The generated token setup has a similar flow, but its request stub must be implemented first.

The repository test project includes `TestPretzel/Assets/Pretzel/Examples/DataEncryptionExample.cs`. Add it to a GameObject and enter Play mode. Its context menu includes an AES demo that needs no configuration, configured AES save/load, RSA report writing, and an **Editor-only** trusted-server read. The no-configuration AES demo uses a disposable random key and temporary folder; real saves must reuse the same backed-up key across launches.

Both **Game-specific keys** exports also create `Assets/Pretzel/Generated/PretzelEncryptionExample.cs`, without needing a sample import. Add it to a GameObject and use its context menu in Play mode for separate AES save/load actions or RSA report writing. It contains no keys and does not run automatically. It uses a dedicated example save folder and includes async token usage, explicit key-source comments and a trusted-server RSA reader. Existing example scripts are preserved on re-export so your edits are not lost. The JSON key remains in Resources; the example script stays in the generated scripts folder.

Shipping creates the same companion beside its setup script. Newly generated `PretzelDataSetup.cs` files also include commented examples; token setups include the sign-in call sequence. Exporting another key does not migrate existing files.

The companion's **Encryption/JSON** context-menu actions explicitly read `GameEncryptionKeyConfig.Load()`; **Encryption/Embedded script** actions explicitly call the provider registered by `PretzelDataSetup` before the first scene. Each source has separate AES save/load and RSA write examples, using separate `Json` and `Embedded` subfolders so testing one source cannot overwrite the other's example saves. These explicit stores do not require the global DataIO format to match; the exported key must match the chosen mode. The embedded actions use whichever provider is currently registered, so run them before replacing it with a token/custom provider. If you already have a companion script, preserve your edits and delete or rename it before re-exporting to get the updated template.
