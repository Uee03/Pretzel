using UnityEngine;

namespace Pretzel.Events
{
    [AddComponentMenu("Pretzel/Events/Bool Event Listener")]
    public sealed class BoolEventListener : EventChannelListener<BoolEventChannel, bool>
    {
    }
}
