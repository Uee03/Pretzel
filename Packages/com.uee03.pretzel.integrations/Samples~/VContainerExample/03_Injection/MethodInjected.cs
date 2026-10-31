using VContainer;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Method injection: useful when a base class or a MonoBehaviour cannot take constructor parameters.</summary>
    public sealed class MethodInjected
    {
        public ISampleLog Log { get; private set; }

        [Inject]
        public void Construct(ISampleLog log) => Log = log;
    }
}
