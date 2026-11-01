namespace Pretzel.Integrations.VContainer
{
    using global::VContainer;
    using global::VContainer.Unity;
    using UnityEngine;

    /// <summary>Project-wide registrations kept in an asset and assigned to a <see cref="PretzelLifetimeScope"/>.</summary>
    public abstract class ScriptableInstaller : ScriptableObject, IInstaller
    {
        public abstract void Install(IContainerBuilder builder);
    }
}
