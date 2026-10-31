using Pretzel.Integrations.VContainer;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>
    /// A ScriptableInstaller: project-wide registrations kept in an asset. Create one with
    /// Assets > Create > Pretzel > Samples > Scriptable Installer and add it to the scope's Installers list.
    /// </summary>
    [CreateAssetMenu(menuName = "Pretzel/Samples/Scriptable Installer", fileName = "SampleScriptableInstaller")]
    public sealed class SampleScriptableInstaller : ScriptableInstaller
    {
        public override void Install(IContainerBuilder builder) => builder.RegisterEntryPoint<ScriptableInstallerDemo>();
    }
}
