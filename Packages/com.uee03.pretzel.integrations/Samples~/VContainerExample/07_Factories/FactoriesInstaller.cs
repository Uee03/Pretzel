using VContainer;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Creating objects at runtime: a Func factory for plain classes and resolver.Instantiate for prefabs.</summary>
    public sealed class FactoriesInstaller : IInstaller
    {
        private const int StartingHealth = 10;

        public void Install(IContainerBuilder builder)
        {
            // Consumers take Func<string, Enemy>. Services are resolved once, the name is passed on every call.
            builder.RegisterFactory<string, Enemy>(resolver =>
            {
                ISampleLog log = resolver.Resolve<ISampleLog>();
                return enemyName => new Enemy(enemyName, StartingHealth, log);
            }, Lifetime.Singleton);

            builder.Register<EnemyViewFactory>(Lifetime.Singleton);
            builder.RegisterEntryPoint<FactoryDemo>();
        }
    }
}
