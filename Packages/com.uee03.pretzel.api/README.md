# Pretzel API

HTTP client on `UnityWebRequest` and `Awaitable`. Depends on `pretzel.core` and `pretzel.data`.

```csharp
ApiClient client = new("https://api.example.com");

Response<string> text = await client.GetAsync("status", cancellationToken: token);
Response<Item> item = await client.GetJsonAsync<Item>("items/1", cancellationToken: token);

if (item.Ok) Use(item.Data);
else Debug.LogWarning($"{item.StatusCode} {item.Error} {item.ErrorBody}");
```

- Instance based, no static state. `BaseUrl` is prepended to relative URLs. `Defaults` holds headers, timeout and retries.
- `GetAsync`, `PostAsync`, `PutAsync`, `DeleteAsync`, `HeadAsync`, `SendAsync`, and `GetJsonAsync`, `PostJsonAsync`, `PutJsonAsync` that go through the `pretzel.data` serializer.
- Downloads: `GetBytesAsync`, `DownloadTextureAsync`, `DownloadAudioAsync`, `DownloadFileAsync` (streams to disk).
- A failed request is a `Response<T>` with `Ok` false, `StatusCode`, `Error`, `ErrorBody` and `TimedOut`. Only cancellation throws.
- `RequestOptions`: per-call timeout, retries with exponential backoff, headers, content type and `IProgress<float>`.
- Call it from the main thread.

For UniTask or R3 versions of these calls, install `pretzel.integrations` and add UniTask or R3.

## Sample

`Samples~/ApiUsage`.
