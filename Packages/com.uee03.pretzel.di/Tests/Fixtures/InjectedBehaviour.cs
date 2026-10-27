using UnityEngine;

namespace Pretzel.DI.Tests
{
    public sealed class InjectedBehaviour : MonoBehaviour
    {
        [Inject] public IObjectResolver Resolver { get; private set; }
    }
}
