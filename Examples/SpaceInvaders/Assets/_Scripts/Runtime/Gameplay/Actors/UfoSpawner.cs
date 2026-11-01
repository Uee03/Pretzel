using Pretzel.Core;
using Pretzel.Core.Timers;
using Pretzel.Events;
using UnityEngine;
using VContainer;

namespace SpaceInvaders.Gameplay
{
    /// <summary>Sends the mystery ship across the top at random intervals while a wave is running.</summary>
    public sealed class UfoSpawner : MonoBehaviour
    {
        [SerializeField] private Ufo _ufo;
        [SerializeField, Min(1f)] private float _minDelaySeconds = 14f;
        [SerializeField, Min(1f)] private float _maxDelaySeconds = 24f;
        [SerializeField] private float _y = 3.45f;

        private readonly CountdownTimer _timer = new(1f);
        private IEventBus _bus;
        private bool _enabled;
        private int _shots;

        public bool IsEnabled => _enabled;

        // Runs when the level scope builds, before Awake: keep it to storing dependencies.
        [Inject]
        public void Construct(IEventBus bus)
        {
            _bus = Guard.NotNull(bus, nameof(bus));
        }

        private void Awake()
        {
            _ufo.Hide();
            _timer.Completed += Launch;
            _ufo.Killed += OnKilled;
            _ufo.Escaped += OnEscaped;
        }

        private void OnDestroy()
        {
            _timer.Completed -= Launch;
            if (_ufo)
            {
                _ufo.Killed -= OnKilled;
                _ufo.Escaped -= OnEscaped;
            }
        }

        private void Update()
        {
            if (_enabled && !_ufo.IsFlying)
            {
                _timer.Tick(Time.deltaTime);
            }
        }

        public void Begin()
        {
            _enabled = true;
            Rearm();
        }

        public void Stop()
        {
            _enabled = false;
            _timer.Reset();
            _ufo.Hide();
        }

        public void LaunchNow()
        {
            if (!_ufo.IsFlying)
            {
                Launch();
            }
        }

        private void Launch()
        {
            _ufo.Launch(_y, Random.value > 0.5f);
        }

        private void OnKilled(Ufo ufo)
        {
            _bus.Publish(new UfoDestroyed(ufo.transform.position, ScoreTable.UfoPoints(_shots++)));
            Rearm();
        }

        private void OnEscaped(Ufo ufo)
        {
            Rearm();
        }

        private void Rearm()
        {
            _timer.Reset(Random.Range(_minDelaySeconds, _maxDelaySeconds));
            _timer.Start();
        }
    }
}
