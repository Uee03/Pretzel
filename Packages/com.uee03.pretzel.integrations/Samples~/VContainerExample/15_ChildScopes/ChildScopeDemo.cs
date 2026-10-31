using System;
using VContainer;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>
    /// Runs three short levels, each in a child scope created from this sample's one LifetimeScope and disposed when
    /// the level ends. Use this when a level, match or popup has no scene of its own.
    /// </summary>
    public sealed class ChildScopeDemo : IStartable, IDisposable
    {
        private const int LevelCount = 3;
        private const float LevelSeconds = 2f;

        private readonly LifetimeScope _scope;
        private readonly ITimerService _timers;
        private Timer _timer;
        private LifetimeScope _level;
        private int _levelNumber;

        // Every scope registers itself as LifetimeScope; this is the one that registered ChildScopeDemo.
        public ChildScopeDemo(LifetimeScope scope, ITimerService timers)
        {
            _scope = scope;
            _timers = timers;
        }

        public void Start() => _timer = _timers.Start(LevelSeconds, NextLevel, repeat: true);

        public void Dispose()
        {
            _timer?.Cancel();
            EndLevel();
        }

        private void NextLevel()
        {
            EndLevel();
            if (_levelNumber == LevelCount)
            {
                _timer.Cancel();
                return;
            }

            _levelNumber++;
            int number = _levelNumber;
            _level = _scope.CreateChild(builder =>
            {
                builder.RegisterInstance(new LevelState(number));
                builder.RegisterEntryPoint<LevelPresenter>(Lifetime.Scoped);
            }, "Level " + number);
        }

        private void EndLevel()
        {
            if (_level != null)
            {
                _level.Dispose();
                _level = null;
            }
        }
    }
}
