# DataIO project setup

## Recommended path

1. Open **Pretzel > Data Format > Setup**.
2. Choose plain/binary setup for unencrypted data, or **Generate AES encryption setup** for
   protected saves. Plain JSON remains the zero-setup default; AES is the only encryption option.
3. For AES, retain and back up the generated key. Embedded C# and bundled JSON both put the key
   in the game; neither makes it inaccessible to a determined player.
4. `ProjectDataIO` initializes itself before the first scene loads. Settings (and the AES key) are
   read once and stay fixed until the game stops; stop Play mode to change them.
5. Run **Test save and load**, then build a player and verify save/quit/relaunch/load.

The test uses an isolated temporary store with configured Editor keys. It does not modify profiles
or prove that a player's build receives its key. **Open store folder** shows the configured default
root, not any custom root or runtime override.

## Which piece owns what?

```text
ProjectSaveManager (component: lifetime, load on start, save on pause/quit)
  -> SaveController<T> (serializable: one kind of data in memory, key, slot, busy guard)
    -> ISaveService / SaveService (load-or-create, save, delete, list slots)
      -> IDataStore = ProjectDataIO.Store
        -> serializer -> plain / binary / AES strategy -> files
```

The format is a strategy inside the store, so one service and controller work with JSON, binary
and AES unchanged. `ProjectDataIO` reads a copy of `DataIOConfig`, loads keys and creates one store.
The settings asset contains no secrets. The generated files belong to you; regenerating is not an
automatic upgrade of existing scripts.

```csharp
[SerializeField] private ProjectSaveManager _saves;

_saves.Profile.Data.coins += 10;
await _saves.Profile.SaveAsync(destroyCancellationToken);
```

Add more data types by adding a field to `ProjectSaveManager` and injecting the same service in `Start`:

```csharp
[SerializeField] private SaveController<GameSettings> _settings = new("settings");
_settings.Inject(Service);
```

For large data (worlds, inventories), split it into several keys, for example one controller per
region, so a save serializes only what changed. Binary keeps such files compact. Serialization runs
on the main thread before the write moves to the background.

`Initialize()` is idempotent and takes no overrides. For keys fetched after sign-in, keep the key
source on `Provider` with no provider registered at startup, then call `InitializeWithKeys(keys)`
once. `ProjectDataIO.WhenReadyAsync` completes when the store exists. Keys cannot be swapped during
a run; restart to sign in with a different key.

## Schema migrations

Implement the partial `ConfigureStore` in a separate file so regenerating `ProjectDataIO.cs` keeps it:

```csharp
public static partial class ProjectDataIO
{
    static partial void ConfigureStore(DataStoreOptions options) => options.AddMigration<Profile>(new ProfileV1ToV2());
}
```

It runs before the store snapshots options. Register every step, including schema 0
for unversioned files. See the package [README](../README.md#schema-upgrades-without-replacing-generated-startup)
for a complete version 1-to-2 field migration. Successfully loading a migration does not rewrite the
file; save it after verifying the loaded value.

The same callback is exposed by `DataIOConfig.CreateStore` and factories.

## Additional stores (advanced)

Prefer folders in keys (`SaveController` folder field) over extra stores. For a store independent
of project settings, create it once and retain it:

```csharp
IDataStore inventory = DataStoreFactory.CreateWithStrategy(
    DataRoots.PersistentData("Inventory"), new BinaryDataStrategy());
```

Changing a strategy or key does not migrate old files. Store locks are per instance, not global;
one coordinator must own a shared path. Await saves at checkpoints. A cancellation token is checked
before background work, not used to abort a file replacement mid-commit. Do not assume an async
save started during quit/destruction will finish.

## Errors and recovery

- `NotFound`: first run or no slot. Create a new profile only deliberately.
- `Corrupt`: inspect `ErrorCode` and `Error`. Wrong key and tampering both mean
  `AuthenticationFailed`; schema and I/O failures have separate categories.
- Missing/invalid keys during initialization throw; repair delivery rather than replacing files.
- Saves throw on failure. Catch expected I/O, access, invalid-data, encryption and setup errors at
  an appropriate UI/event boundary, preserve live progress and offer retry.
- `RecoveredFromBackup` indicates a `.bak` load. Keep backup enabled unless you own another policy.
- Replacement uses a flushed temp and a recoverable rollback fallback where needed, not a universal
  atomic/power-loss guarantee. Do not manually delete recovery files during an active save.

## Examples

The independent buttons generate three components with different payloads and folders:

- Plain text: editable settings.
- Binary: inventory.
- AES: protected profile.

They do not need `ProjectDataIO` and do not replace `DataIO.Default`. Use their context menu in Play
mode. The AES component uses the configured key source.

**Add save manager** additionally generates `ISaveService`, `SaveService`, `SaveController<T>`,
`ProjectSaveManager`, the `ProjectSaveData` profile model, this guide, and (for encryption) a text
encryption demo/Inspector. Add `ProjectSaveManager` to a GameObject in your first scene. Existing
files are never replaced.

## Before shipping

Keep one backed-up AES key across releases. Routine rotation is optional, not a setup requirement.
Read [key delivery](EncryptionKeyDelivery.md) for offline exports and advanced history.
Run limited build checks, then test a real player without Editor-only key files/environment variables.
Test the previous release's saves and actual backend/platform (including IL2CPP if used).
