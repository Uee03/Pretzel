# Manual Wiring With Fakes

| File | What it shows |
|------|---------------|
| `ManualWiringExample.cs` | Every cloud interface wired to an in-memory fake in one composition root: sign-in, an offline save that queues, a flush after reconnecting, a Cloud Code call and a Remote Config read. |
| `ProfileData.cs`, `DoubleRequest.cs`, `DoubleResponse.cs` | The small types it saves and sends. |

This sample has no Unity Gaming Services dependency: it uses `Pretzel.Cloud.InMemory`, which ships in the runtime
assembly. It compiles in every project that has Pretzel Cloud.

## Try it

1. Add `ManualWiringExample` to a GameObject and press Play. The walkthrough is logged to the Console.
2. Read `RunAsync`: it is the only place that knows the fakes. Replace
   `UseAuth(auth).UseSave(save)...` with `UseUnityAuthentication().UseUnityCloudSave()...` (when those packages are
   installed) and the rest of the code does not change.

What the fakes do:

- `InMemoryCloudSaveService` keeps real write-lock semantics (a stale lock is a `Conflict`), so conflict handling is
  exercised without a backend. `Faults.Offline = true` or `Faults.Enqueue(CloudErrorKind.RateLimited, 2)` injects failures.
- `InMemoryCloudAuthService` needs `InitializeAsync` before sign-in and a signed-in player to link an identity.
- `InMemoryCloudCodeService` runs a C# handler per endpoint and passes the request and response through the serializer
  as text, like the real service.
- `InMemoryRemoteConfigService` copies `SetServerValue` values into the current values on `FetchAsync` and keeps the
  cached values when a fetch goes offline.

The fakes are good for tests and offline development. They do not model server-side limits, latency, rate limits you
did not inject, or authentication rules, so a green run against them is not proof the real service accepts your data.
