using VContainer;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Singleton: one per container. Scoped: one per scope. Transient: one per resolve.</summary>
    public sealed class LifetimesInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<SingletonExample>(Lifetime.Singleton);
            builder.Register<ScopedExample>(Lifetime.Scoped);
            builder.Register<TransientExample>(Lifetime.Transient);
            builder.RegisterEntryPoint<LifetimeDemo>();
        }
    }
}
