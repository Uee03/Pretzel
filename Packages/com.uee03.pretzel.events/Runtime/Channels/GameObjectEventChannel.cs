using UnityEngine;

namespace Pretzel.Events
{
    [CreateAssetMenu(menuName = "Pretzel/Events/GameObject Channel", fileName = "GameObjectEventChannel")]
    public sealed class GameObjectEventChannel : EventChannel<GameObject>
    {
    }
}
