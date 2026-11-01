using VContainer;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>A tickable system that moves many pooled objects, fed by a spawner that uses the timer service.</summary>
    public sealed class BulletsInstaller : IInstaller
    {
        private readonly BulletSettings _settings;

        public BulletsInstaller(BulletSettings settings) => _settings = settings;

        public void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_settings);

            // AsSelf keeps BulletSystem resolvable by its own type next to its entry point interfaces.
            builder.RegisterEntryPoint<BulletSystem>().AsSelf();
            builder.RegisterEntryPoint<BulletSpawner>();
        }
    }
}
