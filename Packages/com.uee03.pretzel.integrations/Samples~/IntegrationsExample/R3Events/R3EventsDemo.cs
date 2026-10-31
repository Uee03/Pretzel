using System;
using Pretzel.Events;
using Pretzel.Integrations.R3;
using Pretzel.UI;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.Integrations
{
    /// <summary>
    /// Turns an <see cref="EventBus"/> and a ScriptableObject channel into R3 streams: a running score, a combo that ends after a
    /// pause, and a stream that raises a channel. Game code keeps publishing plain events; only the consumers use R3.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class R3EventsDemo : MonoBehaviour
    {
        private readonly EventBus _bus = new();

        private CompositeDisposable _subscriptions;
        private IntEventChannel _scoreChannel;

        private void OnEnable()
        {
            _subscriptions = new CompositeDisposable();
            _scoreChannel = ScriptableObject.CreateInstance<IntEventChannel>();
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;

            Label score = UIToolkitUtility.CreateLabel("Score: 0", root);
            Label combo = UIToolkitUtility.CreateLabel("Combo: -", root);
            Label channelLabel = UIToolkitUtility.CreateLabel("Channel: -", root);
            Button button = UIToolkitUtility.CreateButton("Defeat an enemy (+10)", null, root);

            // Game code only publishes. This button stands in for it.
            button.ClickedAsObservable()
                .Select(_ => new EnemyDefeated(10))
                .PublishTo(_bus)
                .AddTo(_subscriptions);

            // A running total from the bus stream.
            _bus.AsObservable<EnemyDefeated>()
                .Scan(0, (total, defeated) => total + defeated.Points)
                .Subscribe(total => score.text = "Score: " + total)
                .AddTo(_subscriptions);

            // A combo counts kills and ends after 1.5 seconds without one.
            _bus.AsObservable<EnemyDefeated>()
                .Scan(0, (count, _) => count + 1)
                .Subscribe(count => combo.text = "Combo: x" + count)
                .AddTo(_subscriptions);
            _bus.AsObservable<EnemyDefeated>()
                .Debounce(TimeSpan.FromSeconds(1.5))
                .Subscribe(_ => combo.text = "Combo: ended")
                .AddTo(_subscriptions);

            // The stream raises a ScriptableObject channel, which another stream (or a UnityEvent listener) can observe.
            _bus.AsObservable<EnemyDefeated>()
                .Select(defeated => defeated.Points)
                .RaiseOn(_scoreChannel)
                .AddTo(_subscriptions);
            _scoreChannel.AsObservable()
                .Subscribe(points => channelLabel.text = "Channel raised with " + points)
                .AddTo(_subscriptions);
        }

        private void OnDisable()
        {
            _subscriptions?.Dispose();
            _subscriptions = null;
            if (_scoreChannel)
            {
                Destroy(_scoreChannel);
            }
        }

        private sealed class EnemyDefeated
        {
            public EnemyDefeated(int points)
            {
                Points = points;
            }

            public int Points { get; }
        }
    }
}
