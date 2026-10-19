using System;
using Pretzel.Api;
using Pretzel.Integrations.R3;
using Pretzel.UI;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.Integrations
{
    /// <summary>Turns UI Toolkit events and <see cref="ApiClient"/> calls into R3 streams.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class R3Demo : MonoBehaviour
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

        private void OnEnable()
        {
            _subscriptions = new CompositeDisposable();
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;

            // A running count of button clicks.
            Label counter = UIToolkitUtility.CreateLabel("Clicks: 0", root);
            Button clickButton = UIToolkitUtility.CreateButton("Click me", null, root);
            clickButton.ClickedAsObservable()
                .Scan(0, (count, _) => count + 1)
                .Subscribe(count => counter.text = "Clicks: " + count)
                .AddTo(_subscriptions);

            // Waits for 300 ms of silence before reacting to typing.
            TextField search = UIToolkitUtility.CreateTextField("Search", root);
            Label searched = UIToolkitUtility.CreateLabel("Type something", root);
            search.EventAsObservable<ChangeEvent<string>>()
                .Select(change => change.newValue)
                .Debounce(TimeSpan.FromMilliseconds(300))
                .Subscribe(text => searched.text = "Searched for: " + text)
                .AddTo(_subscriptions);

            // Each click sends one request. Switch drops the previous request if it is still running.
            Label status = UIToolkitUtility.CreateLabel("Idle", root);
            Button fetchButton = UIToolkitUtility.CreateButton("Fetch todo 1", null, root);
            fetchButton.ClickedAsObservable()
                .Select(_ => _client.GetJsonAsObservable<Todo>("todos/1"))
                .Switch()
                .Subscribe(response => status.text = response.Ok ? response.Data.title : "Failed: " + response.Error)
                .AddTo(_subscriptions);
        }

        private void OnDisable()
        {
            _subscriptions?.Dispose();
            _subscriptions = null;
        }
    }
}
