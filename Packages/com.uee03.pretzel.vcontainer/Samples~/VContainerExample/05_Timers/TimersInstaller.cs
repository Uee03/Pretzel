using VContainer;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>A tickable service: every timer in the game advanced from one place.</summary>
    public sealed class TimersInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            // RegisterEntryPoint registers every interface, so TimerService resolves as ITimerService and ticks.
            builder.RegisterEntryPoint<TimerService>();
            builder.RegisterEntryPoint<TimerDemo>();
        }
    }
}
