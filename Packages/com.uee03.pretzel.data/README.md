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
- `DataStore`: the instance that does the work. `new DataStore(root, serializer, extension, options)`. `DataIO` is a thin static facade over `DataIO.Default`, which you can replace or reset with `DataIO.Reset()`.
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

## Set up a project in two steps

Open **Pretzel > Data Format**. The **Setup** tab has two buttons. Each one generates `Assets/Pretzel/DataIO/ProjectDataIO.cs`, a small static class you call once at startup:

| Button | What it does |
|---|---|
| **Generate plain / GZip / BSON setup** | Sets the format and writes the setup code. No keys. |
| **Generate AES / RSA encryption setup** | One step: switches the settings to Encrypted, creates an AES key if there is none, exports the key the game will read (embedded C# or bundled JSON), sets the key source to match, and writes the setup code. For RSA, generate the key pair on the Encryption tab first. |

```csharp
void Start()
{
    ProjectDataIO.Initialize();                 // once, after the key is available
    DataIO.Save("profile", profile);
    LoadResult<Profile> result = DataIO.Load<Profile>("profile");
}
```

Every setting can be changed in two places, whichever suits you:

- **The settings asset** `Assets/Resources/Pretzel/DataIOConfig.asset` (the Format, Encryption and Keys tabs, or its Inspector). It holds no secrets, so it is safe to commit.
- **Code**, for one game start, without touching the asset: `ProjectDataIO.Initialize(config => config.saveFolder = "MyGameSaves");`. The overrides apply to a copy.

`ProjectDataIO` also covers the rest of the lifecycle, so no other initializer is needed:

- `Initialize()` is idempotent. `Shutdown()` forgets the store and resets `DataIO`; it also runs when Play mode starts, so a disabled domain reload never reuses an old session.
- `InitializeWithKeys(keys)` is for keys that arrive after startup, such as a token flow. Keep them in memory.
- `ProjectDataIO.CreateStore("Settings", config => ...)` makes a second store from the same settings in its own folder. This is how you use stores and factories correctly: create each one once, keep it, and use a different folder when the format differs.
- `ProjectDataIO.Describe()` (encrypted setups) names the active key by fingerprint and source, never the key itself.
- RSA setups add `CreateServerStore(folder, serverKeys)` for a trusted server that holds the private key.

The generated file is yours to edit. Regenerating asks before replacing a file that differs, and refuses to create a second copy of the class elsewhere in the project.

**Add examples** (Setup tab) adds a save service, a profile data class and a demo component that use `ProjectDataIO`, plus `DataIOSetup.md` (a longer guide). Encrypted setups also get a text encrypt/decrypt demo with a custom Inspector. Existing files are never replaced. Read [Documentation~/DataIOSetup.md](Documentation~/DataIOSetup.md).

## The Data Format window

- **Setup**: the two buttons above, plus **Add examples** and **Check build setup**.
- **Format**: Plain text, Compressed, Binary or Encrypted, each with pros, cons and when to use it. Serializer, save folder, extension, pretty print, backup and Base64 text.
- **Encryption**: AES-256 or RSA public key + AES-256, RSA key size (2048, 3072 or 4096, with 4096 recommended) and key generation. Generating a new AES key keeps the old one as a previous key.
- **Keys**: the key source, the active key, storage, and the game-specific exports. Store keys as user environment variables, or in the per-project key file `UserSettings/Pretzel/pretzel-keys.json`, which is plain text and must stay out of version control. Environment variables are per user, not per project, so each project gets its own prefix.
- **Shipping**: writes the small `PretzelDataSetup.cs` for environment, embedded or token delivery. It delivers the key only.
- **Decrypt**: paste plain JSON, or the Base64 text of a Compressed, Binary or Encrypted file, to decode it. It can also encode JSON with the current format, and take other keys without saving them.

Generated keys that are not stored yet survive a script reload, and closing the window asks whether to save them to the key file.

## Keys

`DataIOConfig.keySource` decides where encrypted formats look for keys, so a stray environment variable or Editor key file cannot hide a missing key in a build:

| Key source | Reads | Use it for |
|---|---|---|
| `Provider` | `EncryptionKeys.Provider` only: the generated embedded `PretzelDataSetup`, a token flow, or your code | A shipped game (recommended) |
| `BundledJson` | `Assets/Resources/Pretzel/GameEncryptionKeys.json` only | A shipped game, simplest |
| `Environment` | The variables `<PREFIX>_AES_KEY`, `<PREFIX>_RSA_PUBLIC`, `<PREFIX>_RSA_PRIVATE` (Base64) only | Servers and tools. Players do not have these |
| `Automatic` | Provider, then environment, then (in the Editor) the key file, then the bundled JSON | Development. The default |

The Setup button, the Keys tab exports and the Shipping tab set the key source for you. The Keys tab shows which key is active and where it came from, by fingerprint.

```csharp
EncryptionKeys keys = EncryptionKeys.Resolve("PRETZEL_MYGAME", KeySource.Provider, out string origin);
```

**Rotating an AES key.** Generate a new key on the Encryption tab and the old one is kept as a previous key. The key file, the bundled JSON (`previousAesKeys`) and the embedded setup all carry the history. Saves written with an older key still open; new files use the current key, so old files move to it as they are saved again. Once every old save was loaded and saved again, **Clear previous AES keys** on the Keys tab and export again so builds stop carrying them. Environment variables hold the current key only. The bundled JSON also stores a `keyId` fingerprint, and a mismatch is reported as an invalid file.

**Build check.** Before a build, `DataIOBuildCheck` fails with a clear message when an encrypted setup has no key that really ships (the Editor key file and your environment variables are not part of a player), when the bundled JSON is invalid or has the wrong key, or when the embedded setup is for the other mode. It warns that an AES key ships and can be extracted, and about a stale key file. Run it any time with **Tools > Pretzel > DataIO > Check build setup** or **Check build setup** on the Setup tab. Define `PRETZEL_SKIP_DATAIO_CHECK` to turn it off.

**What shipping a key means.** An AES key that ships with the game can be recovered by a determined player, with or without IL2CPP. IL2CPP compiles the code to native code, and the embedded setup masks the key with XOR, but neither makes it secret. It stops casual editing of saves and settings, not a cheater. Keep keys that must stay secret on a server, and fetch a per-player key with a token. Exclude generated AES files (including their `.meta` files) from version control; unlike `DataIOConfig.asset`, they contain key material. Encryption also does not stop rollback: a player can restore an older valid save.

Two encryption modes:

- **AES**: one secret key encrypts and decrypts. The game needs the key, so it is only as safe as the key's delivery.
- **RSA**: each file gets a random AES key that is wrapped with an RSA public key. The game only needs the public key to write. Reading needs the private key, which must never ship with the game. Use it for data you send to a server.

The IV is random per file and stored in the file. There is no IV to store. Read [Documentation~/EncryptionBestPractices.md](Documentation~/EncryptionBestPractices.md) for the best practices behind this (passphrase keys, key rotation, reading files on a server), and [Documentation~/EncryptionKeyDelivery.md](Documentation~/EncryptionKeyDelivery.md) for where keys can live, how to ship them, and how to fetch them with a token.

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

`Samples~/DataUsage`: keys, slots, a ScriptableObject, a migration, an async save, and AES save/load and RSA client/server encryption. For a project's own setup use the **Setup** tab instead; it generates code that fits your settings.

Generated files now go to `Assets/Pretzel/DataIO`. Files from the older `Assets/Pretzel/Generated` folder are not moved or deleted. Move a generated script and its `.meta` there, or remove the old copy after backing it up; the generator refuses to create a second copy of a class.
