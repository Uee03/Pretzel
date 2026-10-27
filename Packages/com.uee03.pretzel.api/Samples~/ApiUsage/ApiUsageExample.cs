using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Pretzel.Api;
using UnityEngine;

namespace Pretzel.Samples.Api
{
    /// <summary>
    /// Add to a GameObject and press Play. Calls a public test API (jsonplaceholder.typicode.com) with every request type.
    /// The texture and audio downloads run only when you enter a URL in the Inspector.
    /// </summary>
    public class ApiUsageExample : MonoBehaviour
    {
        [SerializeField] private string _baseUrl = "https://jsonplaceholder.typicode.com";
        [Tooltip("Optional. An image URL for DownloadTextureAsync.")]
        [SerializeField] private string _textureUrl;
        [Tooltip("Optional. An audio URL for DownloadAudioAsync.")]
        [SerializeField] private string _audioUrl;
        [SerializeField] private AudioType _audioType = AudioType.OGGVORBIS;
        [Tooltip("Optional. Sent as a Bearer token. Never commit a real token to a scene.")]
        [SerializeField] private string _accessToken;

        private readonly CancellationTokenSource _lifetime = new();
        private Texture2D _texture;
        private AudioClip _clip;

        private async void Start()
        {
            ApiClient client = new(_baseUrl);
            client.Defaults.TimeoutSeconds = 10f;
            if (!string.IsNullOrEmpty(_accessToken))
            {
                client.Defaults.SetBearerToken(_accessToken);
            }

            try
            {
                await JsonExamplesAsync(client, _lifetime.Token);
                await TextExamplesAsync(client, _lifetime.Token);
                await DownloadExamplesAsync(client, _lifetime.Token);
            }
            catch (OperationCanceledException)
            {
                // The component was destroyed mid-request.
            }
        }

        private void OnDestroy()
        {
            _lifetime.Cancel();
            _lifetime.Dispose();

            // Downloaded textures and clips are owned by the caller.
            if (_texture)
            {
                Destroy(_texture);
            }

            if (_clip)
            {
                Destroy(_clip);
            }
        }

        private static async Awaitable JsonExamplesAsync(ApiClient client, CancellationToken cancellationToken)
        {
            // Per-call options override the defaults. Retries back off exponentially and only repeat connection errors,
            // timeouts, 408, 429 and 5xx, so use them for idempotent calls such as GET.
            RequestOptions options = new() { Retries = 2, RetryDelaySeconds = 0.5f };
            options.SetHeader("Accept", "application/json");

            Response<Todo> todo = await client.GetJsonAsync<Todo>("/todos/1", options, cancellationToken);
            Debug.Log(todo.Ok ? $"Todo: {todo.Data.title}" : $"GET failed: {todo}");

            Response<Todo> created = await client.PostJsonAsync<Todo, Todo>("/todos", new Todo { title = "Write docs" }, null, cancellationToken);
            Debug.Log($"POST returned {created.StatusCode}, new id {created.Data?.id}.");

            Response<Todo> updated = await client.PutJsonAsync<Todo, Todo>("/todos/1", new Todo { id = 1, title = "Docs written", completed = true }, null, cancellationToken);
            Debug.Log($"PUT returned {updated.StatusCode}, completed {updated.Data?.completed}.");

            // A failed call never throws for HTTP errors. Check Ok and read Error, ErrorBody and TimedOut instead.
            Response<Todo> missing = await client.GetJsonAsync<Todo>("/todos/999999", null, cancellationToken);
            if (!missing.Ok)
            {
                Debug.Log($"Expected failure: {missing.StatusCode}, timed out {missing.TimedOut}, error '{missing.Error}', body '{missing.ErrorBody}'.");
            }
        }

        private static async Awaitable TextExamplesAsync(ApiClient client, CancellationToken cancellationToken)
        {
            Response<string> text = await client.GetAsync("/posts/1", null, cancellationToken);
            Debug.Log($"GET text: {text.Data?.Length ?? 0} characters.");

            Response<byte[]> bytes = await client.GetBytesAsync("/posts/1", null, cancellationToken);
            Debug.Log($"GET bytes: {bytes.Data?.Length ?? 0} bytes.");

            const string body = "{\"title\":\"Hello\",\"body\":\"From Pretzel\",\"userId\":1}";
            Response<string> posted = await client.PostAsync("/posts", body, null, cancellationToken);
            Response<string> put = await client.PutAsync("/posts/1", body, null, cancellationToken);
            Response<string> deleted = await client.DeleteAsync("/posts/1", null, cancellationToken);
            Debug.Log($"POST {posted.StatusCode}, PUT {put.StatusCode}, DELETE {deleted.StatusCode}.");

            // SendAsync takes the method as a value, for code that chooses it at runtime.
            Response<string> sent = await client.SendAsync(HttpMethodType.GET, "/users/1", null, null, cancellationToken);
            Debug.Log($"SendAsync GET: {sent.StatusCode}.");

            Response<IReadOnlyDictionary<string, string>> head = await client.HeadAsync("/posts/1", null, cancellationToken);
            if (head.Ok && head.Headers.TryGetValue("Content-Type", out string contentType))
            {
                Debug.Log($"HEAD Content-Type: {contentType}");
            }
        }

        private async Awaitable DownloadExamplesAsync(ApiClient client, CancellationToken cancellationToken)
        {
            // Progress reports from 0 to 1.
            string file = Path.Combine(Application.temporaryCachePath, "pretzel-sample-todos.json");
            RequestOptions progress = new() { Progress = new Progress<float>(p => Debug.Log($"Download {p:P0}")) };
            Response<string> download = await client.DownloadFileAsync("/todos", file, progress, cancellationToken);
            Debug.Log(download.Ok ? $"Saved {download.Data}" : download.Error);

            if (!string.IsNullOrWhiteSpace(_textureUrl))
            {
                // A URL with a scheme ignores BaseUrl.
                Response<Texture2D> texture = await client.DownloadTextureAsync(_textureUrl, readable: false, null, cancellationToken);
                _texture = texture.Data;
                Debug.Log(texture.Ok ? $"Texture {_texture.width}x{_texture.height}" : texture.Error);
            }

            if (!string.IsNullOrWhiteSpace(_audioUrl))
            {
                Response<AudioClip> audio = await client.DownloadAudioAsync(_audioUrl, _audioType, null, cancellationToken);
                _clip = audio.Data;
                Debug.Log(audio.Ok ? $"Audio clip {_clip.length:0.0}s" : audio.Error);
            }
        }
    }
}
