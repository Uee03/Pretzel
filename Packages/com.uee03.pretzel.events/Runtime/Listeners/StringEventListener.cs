using UnityEngine;

namespace Pretzel.Events
{
    [AddComponentMenu("Pretzel/Events/String Event Listener")]
    public sealed class StringEventListener : EventChannelListener<StringEventChannel, string>
    {
    }
}
