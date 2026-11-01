using UnityEngine;

namespace SpaceInvaders.Gameplay
{
    // Value type: event payload published on the EventBus; no allocation per publish.
    public readonly struct PlayerHit
    {
        public readonly Vector3 Position;

        public PlayerHit(Vector3 position)
        {
            Position = position;
        }
    }
}
