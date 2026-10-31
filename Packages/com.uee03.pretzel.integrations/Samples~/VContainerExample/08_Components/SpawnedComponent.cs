using UnityEngine;
using VContainer;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Created by the container on a new GameObject the first time something needs it.</summary>
    public sealed class SpawnedComponent : MonoBehaviour
    {
        public bool Injected { get; private set; }

        [Inject]
        public void Construct(ISampleLog log) => Injected = log != null;
    }
}
