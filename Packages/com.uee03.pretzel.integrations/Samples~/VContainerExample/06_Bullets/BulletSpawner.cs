using System;
using UnityEngine;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Fires a spiral of bullets on a repeating timer: two tickable services working together.</summary>
    public sealed class BulletSpawner : IStartable, IDisposable
    {
        private const string Topic = "06 Bullets";
        private const float DegreesPerShot = 23f;

        private readonly BulletSystem _bullets;
        private readonly ITimerService _timers;
        private readonly BulletSettings _settings;
        private readonly ISampleLog _log;
        private Timer _fireTimer;
        private Timer _reportTimer;
        private float _angle;

        public BulletSpawner(BulletSystem bullets, ITimerService timers, BulletSettings settings, ISampleLog log)
        {
            _bullets = bullets;
            _timers = timers;
            _settings = settings;
            _log = log;
        }

        public void Start()
        {
            _fireTimer = _timers.Start(_settings.fireInterval, Fire, repeat: true);
            _reportTimer = _timers.Start(3f, () => _log.Write(Topic, $"{_bullets.LiveCount} bullets moved by one Tick"), repeat: true);
        }

        public void Dispose()
        {
            _fireTimer?.Cancel();
            _reportTimer?.Cancel();
        }

        private void Fire()
        {
            _angle += DegreesPerShot;
            float radians = _angle * Mathf.Deg2Rad;
            _bullets.Fire(Vector3.zero, new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f));
        }
    }
}
