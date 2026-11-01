using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Plain C# classes driven by Unity's PlayerLoop, and what happens when one of them throws.</summary>
    public sealed class EntryPointsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            // UseEntryPoints groups entry points; it is the same as calling RegisterEntryPoint for each.
            builder.UseEntryPoints(entryPoints =>
            {
                entryPoints.Add<LifecycleLogger>();
                entryPoints.Add<AsyncStartup>();
                entryPoints.Add<FaultyStartable>();

                // Without a handler an entry point exception is rethrown. One handler serves the whole scope.
                entryPoints.OnException(exception => Debug.LogWarning("[04 Entry points] Handled: " + exception.Message));
            });
        }
    }
}
