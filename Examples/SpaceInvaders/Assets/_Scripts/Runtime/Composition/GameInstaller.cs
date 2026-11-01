using Pretzel.Integrations.VContainer;
using Pretzel.Scenes;
using SpaceInvaders.Audio;
using VContainer;
using VContainer.Unity;

namespace SpaceInvaders
{
    public sealed partial class GameInstaller : ScriptableInstaller
    {
        public override void Install(IContainerBuilder builder)
        {
            InstallGenerated(builder);

            // The loading screen lives under the root scope prefab, so it survives scene loads.
            builder.RegisterComponentInHierarchy<UIDocumentSceneTransition>();
            builder.Register<MasterVolumeSetting>(Lifetime.Singleton);
        }
    }
}
