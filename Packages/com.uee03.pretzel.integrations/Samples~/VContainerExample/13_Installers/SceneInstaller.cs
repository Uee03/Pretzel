using Pretzel.Integrations.VContainer;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>
    /// A MonoInstaller on the scope's GameObject: registrations that need scene references or Inspector values.
    /// PretzelLifetimeScope runs it automatically.
    /// </summary>
    public sealed class SceneInstaller : MonoInstaller
    {
        [SerializeField] private string _welcome = "Registered by a MonoInstaller on the scope";

        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(new WelcomeMessage(_welcome));
            builder.RegisterEntryPoint<WelcomeDemo>();
        }
    }
}
