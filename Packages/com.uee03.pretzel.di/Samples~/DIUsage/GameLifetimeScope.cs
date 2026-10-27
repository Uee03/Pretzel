using Pretzel.DI;
using UnityEngine;

namespace Pretzel.Samples.DI
{
    /// <summary>The scene's composition root. Every registration of the scene is visible in one place.</summary>
    public sealed class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private ScoreView _viewPrefab;

        protected override void Configure(ContainerBuilder builder)
        {
            builder.Register<ScoreService>(Lifetime.Singleton);
            builder.RegisterEntryPoint<PassiveIncome>();
        }

        private void Start()
        {
            // Objects created at runtime are injected before their Awake runs.
            if (_viewPrefab)
            {
                Container.Instantiate(_viewPrefab);
            }
        }
    }
}
