namespace Pretzel.Integrations.VContainer
{
    using global::VContainer;
    using global::VContainer.Unity;
    using UnityEngine;

    /// <summary>Scene registrations with serialized scene references, placed on a <see cref="PretzelLifetimeScope"/> or its children.</summary>
    public abstract class MonoInstaller : MonoBehaviour, IInstaller
    {
        public abstract void Install(IContainerBuilder builder);
    }
}
