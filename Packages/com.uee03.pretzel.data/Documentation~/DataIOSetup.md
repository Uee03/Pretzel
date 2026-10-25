# DataIO project setup

## Recommended path

1. Open **Pretzel > Data Format > Setup**.
2. Choose plain/binary setup for unencrypted data, or **Generate AES encryption setup** for
   protected saves. Plain JSON remains the zero-setup default; AES is the only encryption option.
3. For AES, retain and back up the generated key. Embedded C# and bundled JSON both put the key
   in the game; neither makes it inaccessible to a determined player.
4. Call `ProjectDataIO.Initialize()` once from `Start` or later, before the first `DataIO` call.
5. Run **Test save and load**, then build a player and verify save/quit/relaunch/load.

The test uses an isolated temporary store with configured Editor keys. It does not modify profiles
or prove that a player's build receives its key. **Open store folder** shows the configured default
root, not any custom root or runtime override.

## Which piece owns what?

```text
Gameplay
  -> ProjectSaveService (optional example: your slots and application rules)
    -> DataIO / ProjectDataIO.Store
      -> serializer -> plain / binary / AES strategy -> files
```

`ProjectDataIO` reads a copy of `DataIOConfig`, wires keys and creates one store. You do not need
another factory or initializer for this route. The settings asset contains no secrets.
The generated file belongs to you; regenerating is not an automatic upgrade of existing scripts.

```csharp
ProjectDataIO.Initialize(config => config.saveFolder = "MyGameSaves");
LoadResult<Profile> result = DataIO.Load<Profile>("profile");
switch (result.Status)
{
    case DataStatus.Ok:
        profile = result.Value;
        break;
    case DataStatus.NotFound:
        profile = new Profile();
        break;
    case DataStatus.Corrupt:
        Debug.LogError($"Load failed ({result.ErrorCode}): {result.Error}");
        // Offer recovery/retry. Never overwrite the existing save silently.
        break;
}
```

Parameterless `Initialize()` is idempotent; supplying new settings/keys after initialization is
an error. Finish outstanding saves, call `Shutdown()`, then initialize to change settings.
Shutdown also resets the static facade and runs at Play session startup for disabled domain reload.

For keys fetched after sign-in, use `InitializeWithKeys(keys)` after they arrive. Otherwise the
configured `Provider` or `BundledJson` supplies them. Do not call the lazy `DataIO` default before
your initializer: it would miss your overrides.

## Schema migrations

Use `Initialize(configureStore: options => options.AddMigration<Profile>(...))`.
The callback runs before the store snapshots options. Register every step, including schema 0
for unversioned files. See the package [README](../README.md#schema-upgrades-without-replacing-generated-startup)
for a complete version 1-to-2 field migration. Successfully loading a migration does not rewrite the
file; save it after verifying the loaded value.

The same callback is exposed by generated `CreateStore`, `DataIOConfig.CreateStore`, and factories.

## Additional stores (advanced)

Create once and retain; use distinct folders for different formats:

```csharp
DataStore settings = ProjectDataIO.CreateStore("Settings", config =>
{
    config.format = DataFormat.PlainText;
    config.fileExtension = "json";
});
```

For a store independent of project settings:

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

**Add profile service example** additionally generates a profile model, slot service, demo component,
this guide, and (for encryption) a text encryption demo/Inspector. Use the service as a starting
point, not a required package layer. Existing example files are never replaced.

## Before shipping

Keep one backed-up AES key across releases. Routine rotation is optional, not a setup requirement.
Read [key delivery](EncryptionKeyDelivery.md) for offline exports and advanced history.
Run limited build checks, then test a real player without Editor-only key files/environment variables.
Test the previous release's saves and actual backend/platform (including IL2CPP if used).
