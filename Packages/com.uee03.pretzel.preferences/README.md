# Pretzel Preferences

Player preferences for an options menu, saved through Pretzel Data. Depends on Pretzel Core and Data.

```csharp
PreferencesService preferences = new();              // uses DataIO.Default and the key "preferences"
preferences.Changed += p => audio.SetVolume(AudioChannel.Music, p.audio.music);

LoadResult<PlayerPreferences> result = await preferences.LoadAsync(destroyCancellationToken);
if (result.Status == DataStatus.Corrupt) { /* tell the player their settings were reset */ }
preferences.ApplyGraphics();

preferences.Modify(p => p.graphics.vSyncCount = 0, save: true);
```

| Section | Fields |
|---|---|
| `audio` | `master`, `music`, `sfx`, `ui`, `voice` (0 to 1), `muted`, `subtitles` |
| `graphics` | `qualityLevel`, `width`, `height`, `fullScreenMode`, `vSyncCount`, `targetFrameRate` (-1 or 0 keeps the current value) |
| `controls` | `bindingOverridesJson` (Input System rebinds), `lookSensitivity`, `invertY`, `vibration` |
| `language` | A language code, or empty for the system language |

- A missing or unreadable file falls back to defaults and the load result says which; it never throws for those.
- `GraphicsPreferences.Apply` sets quality, vSync and frame rate everywhere, and resolution and full-screen mode on
  desktop players only. `GraphicsPreferences.Capture()` reads the current values, for example to fill a menu.
- `PlayerPreferences` has `[SchemaVersion(1)]`. When you rename or remove fields in your own copy, add a migration as
  described in the Pretzel Data README. Missing sections in older files are filled with defaults.
- Pass your own `IDataStore` and `DataKey` to keep preferences apart from save games, for example as plain JSON while
  saves are encrypted.

## Sample

`Samples~/PreferencesUsage`: async load, graphics apply and mixer volumes.
