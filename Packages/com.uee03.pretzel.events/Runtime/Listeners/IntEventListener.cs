using UnityEngine;

namespace Pretzel.Events
{
    [AddComponentMenu("Pretzel/Events/Int Event Listener")]
    public sealed class IntEventListener : EventChannelListener<IntEventChannel, int>
    {
    }
}
