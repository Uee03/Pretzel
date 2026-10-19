using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pretzel.Api;
using Pretzel.Integrations.UniTask;
using Pretzel.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.Integrations
{
    /// <summary>Fetches two JSON documents in parallel through <see cref="ApiClient"/> with UniTask, and cancels them from a button.</summary>
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

        private CancellationTokenSource _cts;
        private Label _status;

        private void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _status = UIToolkitUtility.CreateLabel("Idle", root);
            UIToolkitUtility.CreateButton("Fetch todos 1 and 2", () => FetchAsync(), root);
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
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            _status.text = "Loading...";
            try
            {
                (Response<Todo> first, Response<Todo> second) = await UniTask.WhenAll(
                    _client.GetJsonUniTask<Todo>("todos/1", cancellationToken: token),
                    _client.GetJsonUniTask<Todo>("todos/2", cancellationToken: token));

                _status.text = first.Ok && second.Ok
                    ? first.Data.title + "\n" + second.Data.title
                    : "Failed: " + (first.Error ?? second.Error);
            }
            catch (OperationCanceledException)
            {
                _status.text = "Cancelled";
            }
        }
    }
}
