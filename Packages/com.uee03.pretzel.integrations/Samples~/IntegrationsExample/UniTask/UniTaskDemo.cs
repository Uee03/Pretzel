using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pretzel.Api;
using Pretzel.Integrations.UniTask;
using Pretzel.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.Integrations
{
    /// <summary>
    /// Every <see cref="ApiClient"/> UniTask adapter: two JSON GETs in parallel, POSTs, a text GET, a file download and
    /// an optional texture download. One Cancel button stops whatever is running.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class UniTaskDemo : MonoBehaviour
    {
        [Serializable]
        private sealed class Todo
        {
            public int id;
            public string title;
            public bool completed;
        }

        private readonly ApiClient _client = new("https://jsonplaceholder.typicode.com/");

        [Tooltip("Optional. An image URL for DownloadTextureUniTaskAsync.")]
        [SerializeField] private string _textureUrl;

        private CancellationTokenSource _cts;
        private Label _status;

        private void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _status = UIToolkitUtility.CreateLabel("Idle", root);
            UIToolkitUtility.CreateButton("Fetch todos 1 and 2", () => FetchAsync().Forget(), root);
            UIToolkitUtility.CreateButton("Post, download and fetch text", () => SendAsync().Forget(), root);
            UIToolkitUtility.CreateButton("Cancel", () => _cts?.Cancel(), root);
        }

        private void OnDisable()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private async UniTaskVoid FetchAsync()
        {
            CancellationToken token = Restart();

            _status.text = "Loading...";
            try
            {
                (Response<Todo> first, Response<Todo> second) = await UniTask.WhenAll(
                    _client.GetJsonUniTaskAsync<Todo>("todos/1", cancellationToken: token),
                    _client.GetJsonUniTaskAsync<Todo>("todos/2", cancellationToken: token));

                _status.text = first.Ok && second.Ok
                    ? first.Data.title + "\n" + second.Data.title
                    : "Failed: " + (first.Error ?? second.Error);
            }
            catch (OperationCanceledException)
            {
                _status.text = "Cancelled";
            }
        }

        private async UniTaskVoid SendAsync()
        {
            CancellationToken token = Restart();
            _status.text = "Sending...";
            try
            {
                Response<Todo> created = await _client.PostJsonUniTaskAsync<Todo, Todo>("todos", new Todo { title = "From UniTask" }, cancellationToken: token);
                Response<string> raw = await _client.PostUniTaskAsync("todos", "{\"title\":\"Raw body\"}", cancellationToken: token);
                Response<string> text = await _client.GetUniTaskAsync("todos/3", cancellationToken: token);

                string file = Path.Combine(Application.temporaryCachePath, "pretzel-unitask-todos.json");
                Response<string> download = await _client.DownloadFileUniTaskAsync("todos", file, cancellationToken: token);

                string summary = $"POST json {created.StatusCode}, POST text {raw.StatusCode}, GET text {text.Data?.Length ?? 0} chars, file {(download.Ok ? download.Data : download.Error)}";
                if (!string.IsNullOrWhiteSpace(_textureUrl))
                {
                    Response<Texture2D> texture = await _client.DownloadTextureUniTaskAsync(_textureUrl, cancellationToken: token);
                    if (texture.Ok)
                    {
                        summary += $", texture {texture.Data.width}x{texture.Data.height}";
                        // The caller owns downloaded textures.
                        Destroy(texture.Data);
                    }
                }

                _status.text = summary;
            }
            catch (OperationCanceledException)
            {
                _status.text = "Cancelled";
            }
        }

        private CancellationToken Restart()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            return _cts.Token;
        }
    }
}
