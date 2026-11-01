using VContainer;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Child scopes created and disposed at runtime. For additive scenes see LifetimeScope.EnqueueParent in the guide.</summary>
    public sealed class ChildScopesInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder) => builder.RegisterEntryPoint<ChildScopeDemo>();
    }
}
