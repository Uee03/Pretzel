using UnityEngine;

namespace SpaceInvaders.Gameplay
{
    // Value type: event payload published on the EventBus; no allocation per publish.
    public readonly struct InvaderDestroyed
    {
        public readonly InvaderKind Kind;
        public readonly Vector3 Position;
        public readonly int Points;

        public InvaderDestroyed(InvaderKind kind, Vector3 position, int points)
        {
            Kind = kind;
            Position = position;
            Points = points;
        }
    }
}
