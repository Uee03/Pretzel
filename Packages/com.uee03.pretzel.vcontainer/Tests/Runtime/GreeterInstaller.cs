using VContainer;

namespace Pretzel.Integrations.VContainer.Tests
{
    public sealed class GreeterInstaller : MonoInstaller
    {
        public override void Install(IContainerBuilder builder)
        {
            builder.Register<Greeter>(Lifetime.Singleton);
        }
    }
}
