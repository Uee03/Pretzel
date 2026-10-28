using VContainer;
using VContainer.Unity;

namespace Pretzel.Integrations.VContainer.Tests
{
    public sealed class ProbeInstaller : ScriptableInstaller
    {
        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<Probe>();
        }
    }
}
