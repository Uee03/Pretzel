# Options Menu

A complete settings flow on `IPreferencesService`: a runtime options screen, an Apply/Revert draft, and appliers that
push the saved values into the game.

| File | What it shows |
|------|---------------|
| `OptionsMenuController.cs` | A UI Toolkit options screen built in code: sliders, toggles, dropdowns, group boxes and Apply, Revert and Reset buttons. Depends on the interface only. |
| `PreferencesDraft.cs` | An editable copy of the live preferences that is committed with one `Modify` call, so Revert is trivial and `Changed` fires once. |
| `OptionsMenuSetup.cs` | Composition root: loads, applies on load and on every change, and binds the menu. |
| `Appliers/IPreferenceApplier.cs` | One small interface per slice of the game that reacts to preferences. |
| `Appliers/AudioListenerApplier.cs` | Master volume and mute through `AudioListener.volume`. |
| `Appliers/AudioMixerApplier.cs` | Volumes written to exposed AudioMixer parameters, in decibels. |
| `Appliers/LanguageApplier.cs` | Resolves the language (system language when empty) and raises an event only on a real change. |
| `OptionsMenu.uss` | Optional BEM-named styling. |

## Try it

1. Create a GameObject with a `UIDocument`. Assign a PanelSettings and leave the Source Asset empty: the menu builds
   its UI in code.
2. Add `OptionsMenuController` and assign `OptionsMenu.uss` to **Style Sheet**.
3. Add `OptionsMenuSetup` (same or another GameObject) and assign the controller. Optionally assign an AudioMixer with
   exposed volume parameters.
4. Press Play. Change a slider: Apply and Revert enable. Apply saves, applies graphics and runs the appliers.

Rules:

- The menu never stores values itself. The service is the single source of truth, so a second screen or a cloud sync
  that changes the preferences updates the open menu through `Changed`.
- Appliers run on every change. Keep them cheap and idempotent, as `LanguageApplier` is.
- Replace `new PreferencesService()` in `OptionsMenuSetup` with your own `IPreferencesService` (see the Custom
  Preferences Service sample) and nothing else changes.
