using Pretzel.Audio;
using Pretzel.Integrations.VContainer;
using Pretzel.Scenes;
using Pretzel.Sfx;
using Pretzel.Vfx;
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

            // Sounds by name, from the SfxLibrary asset that is registered as a scope setting.
            builder.Register<ISfxPlayer>(resolver => new SfxPlayer(resolver.Resolve<IAudioService>(), resolver.Resolve<SfxLibrary>()), Lifetime.Singleton);

            // Hit stop and slow motion. The service is a component of its own, shared by every scene.
            builder.Register<ITimeScale>(_ => TimeScaleService.Instance, Lifetime.Singleton);

            builder.RegisterEntryPoint<GameInput>().AsSelf();
        }
    }
}
