using UnityEngine;

namespace Pretzel.Events
{
    [CreateAssetMenu(menuName = "Pretzel/Events/String Channel", fileName = "StringEventChannel")]
    public sealed class StringEventChannel : EventChannel<string>
    {
    }
}
