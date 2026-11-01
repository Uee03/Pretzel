using UnityEngine;

namespace SpaceInvaders.Gameplay
{
    // Value type: event payload published on the EventBus; no allocation per publish.
    public readonly struct UfoDestroyed
    {
        public readonly Vector3 Position;
        public readonly int Points;

        public UfoDestroyed(Vector3 position, int points)
        {
            Position = position;
            Points = points;
        }
    }
}
