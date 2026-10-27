using System;
using Pretzel.Events;
using UnityEngine;

namespace Pretzel.Samples.Events
{
    /// <summary>
    /// Owns one <see cref="EventBus"/> for the scene, publishes on key presses and keeps a score from the messages.
    /// In a real project, create the bus in a composition root and pass it to the objects that need it.
    /// </summary>
    public sealed class EventsUsageExample : MonoBehaviour
    {
        [Tooltip("Optional. Raised with the new score, so UnityEvent listeners in the scene can react.")]
        [SerializeField] private IntEventChannel _scoreChanged;

        private readonly EventBus _bus = new();
        private IDisposable _subscription;
        private int _score;

        private void OnEnable()
        {
            _subscription = _bus.Subscribe<EnemyDefeated>(OnEnemyDefeated);
        }

        private void OnDisable()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        [ContextMenu("Defeat Slime")]
        public void DefeatSlime() => _bus.Publish(new EnemyDefeated("Slime", 10));

        [ContextMenu("Defeat Dragon")]
        public void DefeatDragon() => _bus.Publish(new EnemyDefeated("Dragon", 500));

        private void OnEnemyDefeated(EnemyDefeated message)
        {
            _score += message.Points;
            Debug.Log($"{message.EnemyName} defeated, +{message.Points}. Score: {_score}", this);
            if (_scoreChanged)
            {
                _scoreChanged.Raise(_score);
            }
        }
    }
}
