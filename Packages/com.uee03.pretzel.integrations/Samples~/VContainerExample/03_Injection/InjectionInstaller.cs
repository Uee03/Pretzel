using VContainer;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Constructor, method, field and property injection, and injecting objects the container did not create.</summary>
    public sealed class InjectionInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<MethodInjected>(Lifetime.Singleton);
            builder.Register<MemberInjected>(Lifetime.Singleton);
            builder.RegisterEntryPoint<InjectionDemo>();
        }
    }
}
