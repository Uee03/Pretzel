using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Code that runs when the container is built and when it is disposed.</summary>
    public sealed class CallbacksInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.RegisterBuildCallback(resolver => resolver.Resolve<ISampleLog>().Write("14 Callbacks", "RegisterBuildCallback: the container is built"));
            builder.RegisterDisposeCallback(_ => Debug.Log("[14 Callbacks] RegisterDisposeCallback: the container is disposed"));
            builder.RegisterEntryPoint<DisposableResource>();
        }
    }
}
