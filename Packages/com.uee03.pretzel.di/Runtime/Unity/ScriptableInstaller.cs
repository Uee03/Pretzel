using UnityEngine;

namespace Pretzel.DI
{
    /// <summary>Registrations kept in an asset, so several scopes and scenes can share them.</summary>
    public abstract class ScriptableInstaller : ScriptableObject, IInstaller
    {
        public abstract void Install(ContainerBuilder builder);
    }
}
