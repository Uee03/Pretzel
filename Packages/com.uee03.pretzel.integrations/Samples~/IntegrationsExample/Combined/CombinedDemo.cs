using System;
using Cysharp.Threading.Tasks;
using LitMotion;
using Pretzel.Api;
using Pretzel.Integrations.LitMotion;
using Pretzel.Integrations.R3;
using Pretzel.Integrations.UniTask;
using Pretzel.Tween;
using Pretzel.UI;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.Integrations
{
    /// <summary>A click (R3) starts a request (UniTask) and the result slides in with a Pretzel ease (LitMotion).</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class CombinedDemo : MonoBehaviour
    {
        [Serializable]
        private sealed class Todo
        {
            public int id;
            public string title;
            public bool completed;
        }

        private readonly ApiClient _client = new("https://jsonplaceholder.typicode.com/");

        private CompositeDisposable _subscriptions;
        private MotionHandle _handle;
        private Label _card;
        private int _next = 1;

        private void OnEnable()
        {
            _subscriptions = new CompositeDisposable();
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;

            Button button = UIToolkitUtility.CreateButton("Load next todo", null, root);
            _card = UIToolkitUtility.CreateLabel("Nothing loaded", root);
            _card.style.opacity = 0f;

            button.ClickedAsObservable()
                .ThrottleFirst(TimeSpan.FromSeconds(0.5))
                .Subscribe(_ => LoadAsync())
                .AddTo(_subscriptions);
        }

        private void OnDisable()
        {
            _subscriptions?.Dispose();
            _subscriptions = null;
            if (_handle.IsActive())
            {
                _handle.Cancel();
            }
        }

        private async UniTaskVoid LoadAsync()
        {
            Response<Todo> response = await _client.GetJsonUniTaskAsync<Todo>("todos/" + _next++, cancellationToken: destroyCancellationToken);
            _card.text = response.Ok ? response.Data.title : "Failed: " + response.Error;

            if (_handle.IsActive())
            {
                _handle.Cancel();
            }

            _handle = LMotion.Create(0f, 1f, 0.4f)
                .WithEase(EaseType.OutCubic)
                .Bind(t =>
                {
                    _card.style.opacity = t;
                    _card.style.translate = new Translate(0f, (1f - t) * 20f);
                });
        }
    }
}
