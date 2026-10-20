# DataIO in your project

This guide is copied next to `ProjectDataIO.cs` by **Pretzel > Data Format > Setup > Add examples**. It explains how the pieces fit, so you can change them with confidence.

## The layers

```text
Your gameplay code
    -> ProjectSaveService        (optional example: your slot and file rules)
        -> DataIO / ProjectDataIO.Store   (one store, created once at startup)
            -> serializer -> transforms (GZip, BSON, AES/RSA) -> files
```

| Piece | What it does | Do you create it? |
|---|---|---|
| `DataIOConfig` asset | The settings: format, folder, serializer, backup, key source | The Format, Encryption and Keys tabs create and edit it. No secrets |
| `ProjectDataIO` (generated) | Reads the asset, applies your code overrides, creates the store once, installs it as `DataIO.Default` | The Setup tab generates it. Call `Initialize()` once |
| `DataIO` | Static `Save`, `Load`, `Delete`... on the default store | No |
| `IDataStore` / `DataStore` | The object that saves and loads | `ProjectDataIO` makes it. `ProjectDataIO.CreateStore(...)` makes more |
| `DataStoreFactory` | Builds a store from explicit settings, ignoring the asset | Rarely. Only for a store that must not follow the project settings |
| `DataKey` | A name, an optional slot and folder | Yes, when you save |
| `LoadResult<T>` | `Ok`, `NotFound` or `Corrupt` | You read it |

You do not need a factory for the normal path. `ProjectDataIO` does the wiring, including keys.

## Startup

Call `ProjectDataIO.Initialize()` once, from `Start` or later, before any `DataIO` call:

```csharp
void Start()
{
    ProjectDataIO.Initialize();
    // DataIO.Save / DataIO.Load now use the configured store.
}
```

- It is idempotent: calling it again does nothing.
- Change a setting for this start only, without touching the asset: `ProjectDataIO.Initialize(config => config.saveFolder = "MyGameSaves");`. The overrides apply to a copy of the asset's settings.
- Encrypted setups need the key first. With the embedded setup (`PretzelDataSetup`) the key is registered before the first scene, so `Start` is fine. With a token flow, fetch the key first, then call `Initialize()` (key source `Provider`) or `InitializeWithKeys(keys)`.
- `Shutdown()` forgets the store (and keys) and resets `DataIO`. Finish outstanding saves first. It also runs when Play mode starts, so a disabled domain reload never reuses an old session.
- Do not call `DataIO` before `Initialize()`: the lazy default would build a store from the asset without your overrides or keys.

## Changing the configuration

- **In the editor**: the Format tab (format, extension, folder, serializer, backup, Base64), the Encryption tab (mode, key size), the Keys tab (key source, variable prefix), or the `DataIOConfig` asset's Inspector.
- **In code**: the `configure` callback of `Initialize`.
- **Changing the format of a released game** needs a migration plan. A new format or key does not convert old files.

## A second store

Use another store for different data: settings, a cache, reports for a server. Create it once and keep it:

```csharp
DataStore settings = ProjectDataIO.CreateStore("Settings", config =>
{
    config.format = DataFormat.PlainText;
    config.fileExtension = "json";
});
settings.Save("audio", audioSettings);
```

It starts from the same settings (and the same key, when encrypted) in its own folder under the persistent data path. Use a different folder for every differently configured store, so two formats never write the same files.

When a store must not follow the project settings at all, use the factory:

```csharp
DataStore cache = DataStoreFactory.Create(DataRoots.TemporaryCache("Cache"), DataFormat.Compressed);
```

Or construct a `DataStore` directly for custom transforms and migrations:

```csharp
DataStoreOptions options = new() { KeepBackup = true };
options.Transforms.Add(new GZipTransform());
options.AddMigration<Profile>(new DelegateMigration(1, 2, MigrateToV2));
DataStore custom = new(DataRoots.PersistentData("Custom"), new NewtonsoftSerializer(), "json.gz", options);
```

## Handling results

```csharp
LoadResult<Profile> result = DataIO.Load<Profile>("profile");
switch (result.Status)
{
    case DataStatus.Ok:       use(result.Value); break;      // RecoveredFromBackup tells you if .bak was used
    case DataStatus.NotFound: startNewGame(); break;         // keep it in memory until the player saves
    case DataStatus.Corrupt:  offerRetryOrRecovery(); break; // wrong key or a damaged file: never overwrite it silently
}
```

- Save failures throw `IOException`: keep the progress in memory and offer a retry. Do not report success.
- The async methods take a `CancellationToken`. Use a token that outlives the scene for saves that must finish.
- Do not rely on an async save started in `OnApplicationQuit` or `OnDestroy` finishing. Save at checkpoints and await them.
- Several systems writing the same slot need one coordinator. The stores do not serialize concurrent callers.

## Encryption

- **AES**: the game reads and writes with one key.
- **RSA**: the game writes with the public key and cannot read its own files. Send them to your server and read them with `ProjectDataIO.CreateServerStore(folder, serverKeys)`, which needs the private key. Never ship the private key.
- The key source (Keys tab) decides where `ProjectDataIO` looks for the key. A shipped game should use `Provider` (embedded setup or token) or `BundledJson`. `Automatic` is for development.
- `ProjectDataIO.Describe()` shows the active key by fingerprint and source. It never shows the key.
- An AES key that ships with the game can be recovered by a determined player, with or without IL2CPP. This stops casual editing, not a cheater. Keep secrets on a server.
- Encryption does not stop rollback: a player can restore an older valid save.

### Changing the key

Generate a new AES key on the Encryption tab. The old key is kept as a previous key, and exports (the key file, the bundled JSON and the embedded setup) carry it. Old saves still open and move to the new key when saved again. When no old save remains, use **Clear previous AES keys** on the Keys tab and export again. Keep a separate backup of every key your players' saves need.

## Before you ship

- Run **Check build setup** (Setup tab, or Tools > Pretzel > DataIO). A build runs the same check and fails when an encrypted setup has no key that really ships.
- Build a real player (IL2CPP if you use it) with no Editor key file and none of your environment variables, save, quit, start again and load.
- Load a save written by the previous release.
- Exclude generated AES files (`PretzelDataSetup.cs`, `GameEncryptionKeys.json` and their `.meta` files) from public version control. Commit `DataIOConfig.asset`: it has no secrets.
- A wrong key and an edited file both load as `Corrupt`, and your game handles that.

## The example files

- `ProjectSaveData.cs`: what one slot holds. Add your fields. Bump `[SchemaVersion]` and add a migration when their meaning changes.
- `ProjectSaveService.cs`: slots, naming and failure rules in one place, over `IDataStore`. It copies the profile before the async save, so gameplay can keep changing the live object.
- `ProjectSaveExample.cs`: a demo component. Add it to a GameObject, enter Play mode and use its context menu (Load, Add coin, Save). Remove it when you write your own.
- `PretzelEncryptionExample.cs` and its Inspector (encrypted setups): type text, Encrypt, Decrypt. In memory only; do not enter sensitive data into a scene you commit.
