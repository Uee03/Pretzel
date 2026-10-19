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
- **Keys**: store keys as user environment variables, or in the per-project key file `UserSettings/Pretzel/pretzel-keys.json`, which is plain text and must stay out of version control. Environment variables are per user, not per project, so each project gets its own variable prefix.
- **Shipping**: writes `PretzelDataSetup.cs` (environment check, embedded key, or token fetch) and a copy of the key delivery guide into your Assets.
- **Decrypt**: paste plain JSON, or the Base64 text of a Compressed, Binary or Encrypted file, to decode it. It can also encode JSON with the current format, and take other keys without saving them.

Generated keys that are not stored yet survive a script reload, and closing the window asks whether to save them to the key file. In code:

```csharp
EncryptionKeys keys = EncryptionKeys.Resolve("PRETZEL_MYGAME");
DataStore store = DataStoreFactory.Create(DataRoots.PersistentData("SaveData"), DataFormat.Encrypted, keys);
```

`EncryptionKeys.Resolve` takes each key from the first source that has it: `EncryptionKeys.Provider` (your code, for example keys fetched with a token), the environment variables `<PREFIX>_AES_KEY`, `<PREFIX>_RSA_PUBLIC` and `<PREFIX>_RSA_PRIVATE` (Base64), and in the Editor `UserSettings/Pretzel/pretzel-keys.json`. A wrong key, or an edited file, loads as `Corrupt`.

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
