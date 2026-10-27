# API Usage

Add `ApiUsageExample` to a GameObject and press Play. It calls the public test API
`https://jsonplaceholder.typicode.com`, which accepts writes but does not store them.

| Area | Calls |
|------|-------|
| JSON | `GetJsonAsync`, `PostJsonAsync`, `PutJsonAsync`, and a failed request with `Ok`, `StatusCode`, `TimedOut`, `Error`, `ErrorBody` |
| Text and bytes | `GetAsync`, `GetBytesAsync`, `PostAsync`, `PutAsync`, `DeleteAsync`, `SendAsync`, `HeadAsync` |
| Downloads | `DownloadFileAsync` with `Progress`, and `DownloadTextureAsync` / `DownloadAudioAsync` when you enter a URL |
| Options | `Defaults`, per-call `RequestOptions`, `SetHeader`, `SetBearerToken`, `Retries`, `RetryDelaySeconds`, `TimeoutSeconds` |

Rules:

- Pass a `CancellationToken` tied to the owner's lifetime. Cancelling throws `OperationCanceledException`.
- HTTP errors do not throw. Check `Ok`.
- Retry only idempotent calls (GET, PUT, DELETE).
- Destroy downloaded textures and audio clips when you are done with them.
- Keep tokens out of scenes and source control. Load them at runtime.
