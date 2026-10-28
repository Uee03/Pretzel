using VContainer;

namespace Pretzel.Integrations.VContainer.Tests
{
    public sealed class CounterInstaller : ScriptableInstaller
    {
        public override void Install(IContainerBuilder builder)
        {
            builder.Register<ICounter, Counter>(Lifetime.Singleton);
        }
    }
}
