# Cloud Save Usage

| File | What it shows |
|------|---------------|
| `CloudSaveUsageExample.cs` | Anonymous sign-in, an offline-first `SyncedDataStore` over a local `DataStore`, load/save of a profile, a sync status label, and a periodic flush. |
| `PlayerProfile.cs` | The serializable save type. |

This sample compiles only when both the **Authentication** and the **Cloud Save** packages are installed (its asmdef
has the same version defines and define constraints as the adapter assemblies). Without them Unity ignores the folder.

## Try it

1. Link the project to a Unity project in **Edit > Project Settings > Services**, and enable Cloud Save in the Unity
   Dashboard. See the package README for the full setup.
2. Add `CloudSaveUsageExample` to a GameObject. Optionally add a `UIDocument` with a PanelSettings asset and assign it
   for a small panel; without one, status and profile go to the Console.
3. Press Play. The profile loads from the device, then reconciles with the cloud once the sign-in succeeds.
4. Use **Level up and save** (UI) or call `LevelUpAsync` from your own code. Turn the network off, save again, and watch
   the state change to `Offline` with the change queued. It uploads on the next sync.

Rules the sample follows:

- The local store is always written first; the cloud never blocks saving.
- Async work takes `destroyCancellationToken`. The only `async void` members are the Unity `Start` message and UI
  click handlers, and both observe the result in a try/catch.
- Nothing is static and there are no singletons: `RunAsync` is the composition root.
- Sync status is an immutable `SyncStatus` snapshot raised through `SyncedDataStore.StatusChanged`; the sample
  unsubscribes in `OnDestroy`.
- No project id, token or credential is in the code. Unity Services reads the linked project.
