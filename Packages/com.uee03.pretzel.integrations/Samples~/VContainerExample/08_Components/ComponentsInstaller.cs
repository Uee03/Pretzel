using VContainer;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>
    /// Registering MonoBehaviours. RegisterComponent (an existing instance) is in <see cref="SharedInstaller"/>; prefab
    /// spawning is in 07_Factories. UseComponents groups registrations and can parent new GameObjects.
    /// </summary>
    public sealed class ComponentsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.UseComponents(components =>
            {
                components.AddInHierarchy<SceneMarker>();
                components.AddOnNewGameObject<SpawnedComponent>(Lifetime.Singleton, "Spawned Component");
            });

            builder.RegisterEntryPoint<ComponentsDemo>();
        }
    }
}
