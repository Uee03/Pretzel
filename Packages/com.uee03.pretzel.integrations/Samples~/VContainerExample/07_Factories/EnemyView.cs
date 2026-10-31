using UnityEngine;
using VContainer;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>A prefab component that needs a service. Instances made with resolver.Instantiate are injected.</summary>
    public sealed class EnemyView : MonoBehaviour
    {
        private ISampleLog _log;

        [Inject]
        public void Construct(ISampleLog log) => _log = log;

        private void Start() => _log.Write("07 Factories", $"{name} spawned and injected before Start");
    }
}
