using Pretzel.Audio;
using Pretzel.Integrations.VContainer;
using VContainer;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>
    /// A MonoInstaller for the optional audio demo. Add it to the scope's GameObject (next to <see cref="SceneInstaller"/>) when
    /// Pretzel Audio is installed; PretzelLifetimeScope runs it automatically. It registers the audio component under its
    /// interface, so everything that needs sound asks for <see cref="IAudioService"/> and never sees the component.
    /// </summary>
    public sealed class AudioSceneInstaller : MonoInstaller
    {
        public override void Install(IContainerBuilder builder)
        {
            // The component lives on its own GameObject, created when the container is built.
            builder.RegisterComponentOnNewGameObject<AudioService>(Lifetime.Singleton, "Audio Service").As<IAudioService>();
            builder.RegisterEntryPoint<AudioDemo>();
        }
    }
}
