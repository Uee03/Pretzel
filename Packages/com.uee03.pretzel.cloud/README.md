# Pretzel Cloud

Unity Gaming Services (UGS) for Pretzel: Authentication, Cloud Save, Cloud Code and Remote Config behind interfaces that
reference no SDK, plus an offline-first cloud save store that plugs into Pretzel Data. Depends on Pretzel Core and
Pretzel Data. It does **not** depend on any UGS package: each adapter assembly compiles only when its Unity package is
installed, the same way Pretzel Integrations handles optional libraries.

```csharp
// Composition root. The Use... extensions exist only for the UGS packages that are installed.
CloudServices services = CloudServices.CreateBuilder()
    .UseUnityAuthentication()
    .UseUnityCloudSave()
    .Build();

await services.ConnectAsync(destroyCancellationToken);               // initialize + sign in anonymously, with retry

// Route Pretzel Data through the cloud. The local store keeps working with no network.
SyncedDataStore store = services.CreateSyncedStore(DataIO.Default);
DataIO.Default = store;

DataIO.Save("profile", profile);                                      // device first, queued for the cloud
LoadResult<Profile> loaded = await DataIO.LoadAsync<Profile>("profile", destroyCancellationToken);
await store.FlushAsync(destroyCancellationToken);                     // upload what is queued
```

## Which package enables which adapter

| Unity package | Version compiled against | Adapter assembly | Interface it implements | `CloudServicesBuilder` extension |
|---|---|---|---|---|
| `com.unity.services.authentication` | 3.8.0 | `Pretzel.Cloud.Authentication` | `ICloudAuthService` | `UseUnityAuthentication` |
| `com.unity.services.cloudsave` | 3.4.2 | `Pretzel.Cloud.CloudSave` | `ICloudSaveService` | `UseUnityCloudSave` |
| `com.unity.services.cloudcode` | 2.10.4 (below 3.0.0) | `Pretzel.Cloud.CloudCode` | `ICloudCodeService` | `UseUnityCloudCode` |
| `com.unity.remote-config` | 4.2.5 | `Pretzel.Cloud.RemoteConfig` | `IRemoteConfigService` | `UseUnityRemoteConfig` |

- Each adapter's asmdef has a `versionDefines` entry with the version above as the minimum and the same symbol in
  `defineConstraints`. With the package missing, or older than the version listed, the assembly is silently absent and
  everything else still compiles. `com.unity.services.core` comes in through those packages (1.18.0 when compiled).
- Cloud Code 3.0.0 needs Unity 6000.7 and was not compiled against; the adapter is limited to versions below 3.0.0.
- Cloud Save and Cloud Code need a signed-in player, so install Authentication too.
- Pretzel Cloud declares Unity 6000.0 as its minimum like the other Pretzel packages, but it was compiled and tested on
  6000.6.5f1 only. Check the minimum Unity version of the UGS package you install.
- Without any UGS package you still get the interfaces, `SyncedDataStore`, retry, conflict resolution and the in-memory
  fakes (`Pretzel.Cloud.InMemory`).

## Set up a Unity project

1. Install the packages you need from the Package Manager (Unity Registry).
2. Link the project: **Edit > Project Settings > Services**, sign in to your Unity account and link or create a project.
3. In the Unity Dashboard enable the services you use (Cloud Save, Cloud Code, Remote Config). Create Remote Config keys
   and deploy Cloud Code modules there; this package ships no server code.
4. Unity Services initialize in **Play Mode and builds only**: `UnityServices.InitializeAsync` throws in Edit Mode, and
   the adapters report that as `CloudErrorKind.NotInitialized`.
5. No project id, key or token goes in your code; Unity reads the linked project. Never log `CloudIdentity` tokens.

## Architecture

| Layer | Types |
|---|---|
| Results and errors | `CloudResult`, `CloudResult<T>`, `CloudError`, `CloudErrorKind`, `CloudErrorMapper`, `CloudSerialization` |
| Retry | `RetryPolicy` (pure backoff and jitter rules), `CloudRetry` (runs an operation with the policy), `IRetryDelay`, `AwaitableRetryDelay`, `TaskCancellation` |
| Service interfaces | `ICloudAuthService`, `ICloudSaveService`, `ICloudCodeService`, `IRemoteConfigService` |
| Offline-first sync | `SyncedDataStore`, `SyncedDataStoreOptions`, `SyncStatus`, `SyncState`, `SyncJournal`, `SyncPlanner`, `IConflictResolver` and three resolvers, `CloudKeyMapper`, `CloudPayload` |
| Composition | `CloudServices`, `CloudServicesBuilder` |
| Fakes | `InMemoryCloudAuthService`, `InMemoryCloudSaveService`, `InMemoryCloudCodeService`, `InMemoryRemoteConfigService`, `CloudFaults` |

Methods return failures as `CloudResult` values and throw only for cancellation (`OperationCanceledException`) and for
programming errors such as a null argument. Callers branch on `CloudErrorKind` and never reference SDK exception types.
Asynchronous methods return `Awaitable`, like Pretzel Data, and are for the main thread.

### Errors

Adapters map SDK exceptions to `CloudErrorKind`: `Offline`, `Timeout`, `NotInitialized`, `NotSignedIn`, `Unauthorized`,
`RateLimited` (with `RetryAfter`), `ServiceUnavailable`, `NotFound`, `Conflict`, `InvalidRequest`, `Serialization`,
`ScriptError` and `Unknown`. `CloudError.IsTransient` is true for offline, timeout, rate limited and unavailable;
`CloudError.NeedsSignIn` for not signed in, unauthorized and not initialized. The shared table lives in
`CloudErrorMapper` and is unit tested without any SDK.

### Retry and cancellation

`RetryPolicy` retries only transient errors, with exponential backoff and jitter, honoring a server `RetryAfter` up to
`MaxDelay`. `CloudRetry.RunAsync` waits through an `IRetryDelay` (unscaled time by default) and stops with an
`OperationCanceledException` when the token is cancelled, also during a backoff wait.

The UGS SDKs take no `CancellationToken`. The adapters stop **waiting** when you cancel, but a request already sent may
still reach the service. A cancelled upload that actually succeeded is detected on the next sync because the cloud copy
carries the same save time as the queued device copy.

## Cloud saves with Pretzel Data

`SyncedDataStore` is an `IDataStore` over a local `IDataStore` (a file `DataStore`, encrypted or not) and an
`ICloudSaveService`. Use it anywhere an `IDataStore` fits: `DataIO.Default`, `SaveSlots<T>`, `AutoSaveTrigger`,
preferences and so on.

```csharp
DataIO.Default = services.CreateSyncedStore(DataIO.Default);          // once, at startup
```

`DataIO.Default` resets when Play mode starts, so assign it at startup and not in an editor script.

| Call | Behavior |
|---|---|
| `Save` / `Delete` | Writes the local store, then queues the change. Never touches the network. |
| `Load`, `LoadInto`, `Exists`, `List`, `ListSlots` | Local store only. |
| `SaveAsync` / `DeleteAsync` | Local first, queue, then (by default) one upload attempt. A cloud failure never throws; it shows in `Status` and the change stays queued. |
| `LoadAsync` / `LoadIntoAsync` | Reads the device copy and the cloud copy, reconciles, and returns the winner. Offline, it returns the device copy. |
| `ExistsAsync`, `ListAsync`, `ListSlotsAsync` | Device keys joined with cloud-only keys, so a new device can discover saves. |
| `FlushAsync` | Uploads the whole queue in the order the changes were made, with `FlushRetry`. |
| `RunSyncLoopAsync(interval, token)` | Flushes on an interval until the token is cancelled. Start it from a component and await it with a try/catch, or write your own loop as the Cloud Save Usage sample does. |

The cloud item for a key holds a `CloudPayload`: the serialized value, the device save time (UTC ticks) and the
`[SchemaVersion]` of the type. Cloud keys are `pz-{slot}-{folder}-{name}` with every character other than letters and
digits written as `_XX` (UTF-8 hex), because Cloud Save keys allow only letters, digits, `-` and `_`.
`CloudKeyMapper` converts both ways; a key that is too long once encoded makes `Save` throw before anything is written.
Use `SyncedDataStoreOptions.SyncFilter` to keep keys such as thumbnails on the device only.

A fresh install does not know the cloud keys. Call `ListAsync` or `ListSlotsAsync`, then `LoadAsync` for the keys you
need (`SaveSlots<T>.LoadAsync` does this for a slot).

### Schema versions

Cloud payloads are migrated with the same `IMigration` steps as files. Register them in `SyncedDataStoreOptions`
(`AddMigration<T>`) as well as in `DataStoreOptions`. A cloud copy saved with a newer schema than this build, or an older
one without a registered migration, is **not applied**: the device copy is kept and `Status.LastError` is
`Serialization`.

### Conflict policy

A conflict exists only when the device and the cloud both changed since they last agreed. A side that merely fell behind
fast-forwards without asking the resolver. `SyncPlanner.Plan` holds the rules and is tested on its own.

| Resolver | Behavior |
|---|---|
| `NewestWinsConflictResolver` (default) | The later device save time wins. A tie keeps the device copy. |
| `KeepLocalConflictResolver` | The device copy always wins and is uploaded. |
| `KeepCloudConflictResolver` | The cloud copy always wins; unsynced device changes are discarded. |
| Your own `IConflictResolver` | Receives a `ConflictInfo` (key, both save times, whether the device change is a delete). |

- Save times come from the device clock (`SyncedDataStoreOptions.Clock`). A device with a wrong clock can lose or win
  "newest wins" unfairly; use `KeepCloud` or a custom resolver when the server must decide. A save always gets a time
  later than the previous save of the same key.
- Data saved before syncing existed has an unknown device time (0). With newest-wins it loses to any cloud copy; use
  `KeepLocal` for the first upload of existing progress, or accept the cloud copy.
- A device copy that wins a conflict is uploaded with a save time newer than the cloud copy it replaces.
- Uploads send the last known write lock. If another device wrote in between, the service answers `Conflict`; the store
  reloads the cloud copy and applies the same policy, then retries once.
- A queued delete that meets newer cloud data is a conflict too (`ConflictInfo.LocalIsDeletion`).
- A delete made on another device does not reach a device that still holds the key: it finds the key missing in the
  cloud and uploads it again.

### Offline behavior

- Saves always succeed locally. The queue is saved through the local store (`PretzelCloud/sync-journal`), so it survives
  restarts and is picked up by the next `SyncedDataStore` on the same local store.
- Several changes of one key are coalesced into the latest. Different keys upload in the order they were changed, and a
  transient failure stops the flush at that key so later changes never overtake it.
- A key the service rejects for good (invalid key, key limit) is parked as failed and does not block the others; saving
  that key again retries it. `Status.FailedCount` counts them.
- `Status` is an immutable `SyncStatus` (`Synced`, `Pending`, `Syncing`, `Offline`, `NotSignedIn`, `Error`, queue size,
  last error, last success time, resolved conflicts). `StatusChanged` fires on the main thread.
- Queued payloads are stored in the journal file, which suits small player data. Keep large blobs out of the synced store.
- A journal that cannot be read is replaced by an empty one; unsynced changes are then treated as device data of unknown
  age on the next load.
- `FlushRetry` (default 3 attempts) is used by `FlushAsync`; `ForegroundRetry` (default one attempt) by `SaveAsync`,
  `LoadAsync` and listing, so a bad connection never stalls a menu.

## Authentication, Cloud Code and Remote Config

```csharp
await services.Auth.SignInWithIdentityAsync(new CloudIdentity(CloudIdentityProvider.Google, idToken));
await services.Auth.LinkIdentityAsync(new CloudIdentity(CloudIdentityProvider.Apple, idToken));   // keep an anonymous player's progress

CloudResult<Reply> reply = await services.Code.CallModuleAsync<Request, Reply>("Module", "Function", new Request { amount = 3 }, token);
CloudResult<RemoteConfigFetchInfo> fetched = await services.RemoteConfig.FetchAsync(userAttributes, null, token);
int maxEnemies = services.RemoteConfig.GetInt("max_enemies", 10);
GameTuning tuning = services.RemoteConfig.GetJson("tuning", new GameTuning());
```

- Cloud Code: the request is serialized with the builder's `ISerializer` and sent as the endpoint's named arguments, so
  its JSON must be an object whose keys are the endpoint's parameter names. The response is read as JSON and deserialized
  with the same serializer.
- Remote Config: a failed network fetch keeps cached values and still succeeds; `IsFresh` and `Origin` tell them apart.
  Getters return your default for a missing key or another type.
- `UseUnityAuthentication(profile, environment)` sets a Unity Services profile and environment;
  `UseUnityAuthenticationWithOptions(InitializationOptions)` exposes the SDK type for anything else.

## Fakes and testing

`Pretzel.Cloud.InMemory` ships four fakes with failure injection (`Faults.Offline`, `Faults.Enqueue(kind, count)`).
`InMemoryCloudSaveService` keeps real write-lock semantics. Combine them with a `DataStore` in a temp folder
(`DataRoots.Absolute`) to test a whole save flow with no network. See the Manual Wiring With Fakes sample.

## Samples

- `Samples~/CloudSaveUsage`: anonymous sign-in, an offline-first profile, a status label and a periodic sync. Needs
  Authentication and Cloud Save.
- `Samples~/CloudCodeAndRemoteConfig`: a module call and typed Remote Config reads. Needs Authentication, Cloud Code
  and Remote Config.
- `Samples~/ManualWiringWithFakes`: the same interfaces on in-memory fakes, no UGS package needed.

## What is and is not verified

Verified: the package compiles with zero errors and warnings against the UGS versions in the table above and also with
none installed, in a Unity 6000.6.5f1 host; the EditMode tests (retry and backoff, error mapping, conflict rules, key
encoding, the queue, `SyncedDataStore` against the fakes and a real `DataStore` in a temp folder, cancellation,
Cloud Save adapter calls against a stand-in for the SDK service, SDK exception mapping) pass; the samples compile.

Live smoke tests in the host project (`TestPretzel/Assets/_Project/Tests/LiveServices`, PlayMode, category
`LiveServices`, ignored when the project is not linked) passed against a real Unity Services project on 2026-10-31:
anonymous sign-in, a Cloud Save save/load/delete round trip, and a Cloud Code call to a missing module failing as a result.

Not verified: the package EditMode tests run with no network and no Unity project link. Beyond the smoke tests above,
how `CallModuleEndpointAsync<JToken>` deserializes real module output, Remote Config against the live service and in a
built player, the Authentication link flows, and any platform
(IL2CPP, mobile, WebGL) were not run. Cloud Save limits (letters, digits, `-` and `_` in keys up to 255 characters,
2000 keys and 5 MB per player) come from Unity's documentation, not from a test. Rate limits, quotas and the clock
accuracy of your players are yours to measure. Player-build behavior and allocation were not measured.
