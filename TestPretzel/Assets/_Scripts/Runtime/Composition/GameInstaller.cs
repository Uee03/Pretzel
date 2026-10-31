using Pretzel.Integrations.VContainer;
using VContainer;
using VContainer.Unity;

namespace TestPretzel
{
    public sealed partial class GameInstaller : ScriptableInstaller
    {
        public override void Install(IContainerBuilder builder)
        {
            InstallGenerated(builder);
        }
    }
}
