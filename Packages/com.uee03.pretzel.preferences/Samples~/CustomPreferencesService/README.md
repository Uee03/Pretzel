# Custom Preferences Service

`IPreferencesService` is the contract that consumers depend on. `PreferencesService` (Pretzel Data) is the default
implementation. This sample shows two others and a composition root that picks one.

| File | What it shows |
|------|---------------|
| `PlayerPrefsPreferencesService.cs` | A full `IPreferencesService` that stores the preferences as JSON in PlayerPrefs, including `LoadResult` values for missing and unreadable data. |
| `InMemoryPreferencesService.cs` | An implementation that never touches disk, for tests, kiosks and stand-ins. |
| `PreferencesBackend.cs` | The enum the bootstrap uses to choose a backend. |
| `PreferencesBootstrap.cs` | The only class that knows the concrete service. Loads at startup and raises `Ready` with the interface. |
| `PreferencesChangeLogger.cs` | A consumer that only sees `IPreferencesService`; swap the backend and it keeps working. |

## Try it

1. Create a GameObject, add `PreferencesBootstrap` and `PreferencesChangeLogger`.
2. Pick a **Backend** on the bootstrap and press Play. The console prints the loaded preferences.
3. Use the `PreferencesChangeLogger` context menu **Toggle Mute And Save**, stop, press Play again: PlayerPrefs and
   Pretzel Data keep the change, In Memory forgets it.

Writing your own service:

- Raise `Changed` after every load, modification and reset, and always pass `Current`. Never leave `Current` null.
- Return `LoadResult.NotFound()` for missing data and `LoadResult.Corrupt(...)` for unreadable data, and fall back to
  defaults in both cases so callers never see a null.
- Fill in missing sections after loading, because files from older versions can lack them.
- Return completed `Awaitable`s through `AwaitableCompletionSource` when the backend is synchronous.
