# Preferences Usage

| File | What it shows |
|------|---------------|
| `PreferencesUsageExample.cs` | Async load at startup, reporting a corrupt file, applying graphics, and mapping volumes to an `AudioMixer` on every change. |

## Try it

1. Add `PreferencesUsageExample` to a GameObject. Optionally assign an `AudioMixer` with exposed parameters named
   `MasterVolume`, `MusicVolume` and `SfxVolume`.
2. Press Play, then use **Lower Music And Save** from the context menu. The file is written through `DataIO.Default`
   (the format chosen in **Pretzel > Data Format**) as `preferences`.

Rules:

- Load once at startup and keep one `PreferencesService` for the session, for example in a DI container.
- `Load` never throws for a missing or corrupt file. It falls back to defaults and returns why.
- `GraphicsPreferences.Apply` changes resolution and full-screen mode on desktop players only; quality, vSync and
  frame rate apply everywhere.
- Store Input System rebinds as JSON in `controls.bindingOverridesJson`
  (`InputActionAsset.SaveBindingOverridesAsJson` and `LoadBindingOverridesFromJson`).
- If you use Pretzel Audio, forward volumes with `AudioManager.SetVolume` in the `Changed` handler instead.
