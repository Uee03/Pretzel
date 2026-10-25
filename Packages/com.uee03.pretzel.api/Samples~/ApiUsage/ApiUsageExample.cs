using System;
using System.Threading;
using Pretzel.Api;
using UnityEngine;

namespace Pretzel.Samples.Api
{
    public class ApiUsageExample : MonoBehaviour
    {
        private readonly CancellationTokenSource _lifetime = new();

        private async void Start()
        {
            ApiClient client = new("https://jsonplaceholder.typicode.com");
            client.Defaults.TimeoutSeconds = 10f;

            // Per-call options override the defaults. Retries use exponential backoff.
            RequestOptions options = new() { Retries = 2 };
            options.SetHeader("Accept", "application/json");

            try
            {
                Response<Todo> todo = await client.GetJsonAsync<Todo>("/todos/1", options, _lifetime.Token);
                Debug.Log(todo.Ok ? $"Todo: {todo.Data.title}" : $"GET failed: {todo}");

                Response<Todo> created = await client.PostJsonAsync<Todo, Todo>("/todos", new Todo { title = "Write docs" }, null, _lifetime.Token);
                Debug.Log($"POST returned {created.StatusCode}, new id {created.Data?.id}.");

                string file = System.IO.Path.Combine(Application.temporaryCachePath, "sample.json");
                Response<string> download = await client.DownloadFileAsync("/todos", file, new RequestOptions { Progress = new Progress<float>(p => Debug.Log($"{p:P0}")) }, _lifetime.Token);
                Debug.Log(download.Ok ? $"Saved {download.Data}" : download.Error);
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
        }
    }
}
