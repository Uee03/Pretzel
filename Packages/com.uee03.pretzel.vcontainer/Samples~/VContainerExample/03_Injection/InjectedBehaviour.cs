using UnityEngine;
using VContainer;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>MonoBehaviours have no constructor, so they receive services through an injection method.</summary>
    public sealed class InjectedBehaviour : MonoBehaviour
    {
        public ISampleLog Log { get; private set; }

        [Inject]
        public void Construct(ISampleLog log) => Log = log;
    }
}
