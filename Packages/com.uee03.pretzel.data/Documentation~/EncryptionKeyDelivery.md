# Delivering the AES key

For most offline games, choose **embedded C#** or **bundled JSON** on the Setup tab, back up the
key, and keep using it across releases. Rotation and server delivery are advanced options, not
requirements. See [setup](DataIOSetup.md) and [AES guidance](EncryptionBestPractices.md).

> Any AES key the client can use can be extracted by a determined player. Bundling, masking and
> IL2CPP discourage casual inspection; they do not make a client-held key secret.

## Sources

`DataIOConfig.keySource` selects exactly where startup looks:

| Source | Reads | Suitable for |
|---|---|---|
| `Provider` | `EncryptionKeys.Provider` only | Generated embedded setup or your own provider |
| `BundledJson` | `Resources/Pretzel/GameEncryptionKeys.json` only | Simple offline games |
| `Environment` | `<PREFIX>_AES_KEY`, Base64 encoding of 32 bytes | Managed machines, tools, tests |
| `Automatic` | Provider, environment, Editor key file, bundled JSON | Development |

Automatic resolution can merge missing key material/history from later sources. Explicit sources
avoid an Editor-only key masking missing player delivery. Invalid present environment keys throw;
they are not treated as absent.
Use a project-specific prefix. Environment variables belong to the user/machine, not the project.

The plaintext Editor key file is `UserSettings/Pretzel/pretzel-keys.json`; it never ships.
Protect it, keep `UserSettings` ignored, and maintain a separate recoverable backup.

## Offline delivery

### Bundled JSON

Setup or Keys exports `Assets/Resources/Pretzel/GameEncryptionKeys.json` and selects `BundledJson`.
It includes the AES key, fingerprint and optional previous keys. Malformed content or a mismatched
fingerprint is rejected. Unity includes the TextAsset in the player.

### Embedded C#

Setup or Shipping generates `PretzelDataSetup.cs` and selects `Provider`.
It reconstructs XOR-masked key arrays and registers a provider at `AfterAssembliesLoaded`, before
`ProjectDataIO` initializes at `BeforeSceneLoad`. Re-export older scripts that register at
`BeforeSceneLoad`; **Check build setup** reports them.

Both options put the same usable AES secret on the player's machine. Keep key-bearing exports and
their `.meta` files out of public version control: ignore `PretzelDataSetup.cs` (or the JSON file)
by name, not the whole output folder, because `ProjectDataIO.cs` lives there too and must be committed.
Regenerating never silently replaces a `PretzelDataSetup.cs` that contains your own code.
Store a protected copy for reproducible builds.
Remove unused exports: an unselected JSON file still ships if left in Resources.
Restart Play mode after re-exporting; the running store keeps the keys it started with.

## Custom in-memory delivery (advanced)

When you already obtain a 32-byte key from a trusted source:

```csharp
EncryptionKeys keys = new() { AesKey = receivedKey };
ProjectDataIO.InitializeWithKeys(keys);
```

Or set `EncryptionKeys.Provider = () => keys` at `AfterAssembliesLoaded` and select `Provider`.
With `Provider` selected and no provider registered at startup, `ProjectDataIO` waits for one of
these calls. Do not save the key in PlayerPrefs or print it in logs.

The Shipping token scaffold is optional. Implement `RequestKeyAsync`; the placeholder throws and
**Check build setup** fails the build until it is replaced. `LoadKeysAsync` returns `false` (and logs
why) on a failed request or an invalid key, and rethrows cancellation.
A service must authenticate and authorize requests, use HTTPS, and keep per-player keys stable
across sessions. Handle failure/cancellation before initializing or touching existing saves.
Settings and keys stay fixed for the run: signing in as a different player requires a restart, so
the next user never reads with the previous user's key in the same session.
`EncryptionKeys.Provider` is also cleared whenever Play mode starts.
Server delivery is not a promise of offline availability or protection against client memory access.

## Optional history

Advanced key controls can create a new current AES key while keeping previous keys for reads.
Back up and export the full history when changing keys. Writes use the current key; old files use
their matching history key until re-saved. Remove previous keys only after every supported old save
has migrated. Environment variables contain the current key only.

Most games should keep the original key rather than introducing this lifecycle unnecessarily.

## Verify delivery

1. Run **Test save and load** with the configured source. No new key is created by diagnostics.
2. Run **Check build setup** (**Tools > Pretzel > DataIO > Check build setup**). It checks recognizable
   generated delivery/settings, not arbitrary
   startup execution or remote availability.
3. Build a real player without the Editor key file or development environment variables.
4. Save, quit, relaunch and load; also load representative previous-release saves.
5. Verify the target scripting backend/platform. Editor success is not IL2CPP/platform evidence.
