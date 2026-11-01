using UnityEngine;

namespace SpaceInvaders.Gameplay
{
    /// <summary>The side-to-side march: step sideways until the next step would leave the field, then drop and turn around.</summary>
    public sealed class MarchLogic
    {
        public float Direction { get; private set; } = 1f;

        public bool LastStepDropped { get; private set; }

        public void Reset()
        {
            Direction = 1f;
            LastStepDropped = false;
        }

        /// <param name="minX">Left edge of the leftmost invader.</param>
        /// <param name="maxX">Right edge of the rightmost invader.</param>
        public Vector2 NextStep(float minX, float maxX, float leftLimit, float rightLimit, float stepX, float dropY)
        {
            bool blockedRight = Direction > 0f && maxX + stepX > rightLimit;
            bool blockedLeft = Direction < 0f && minX - stepX < leftLimit;
            if (blockedRight || blockedLeft)
            {
                Direction = -Direction;
                LastStepDropped = true;
                return new Vector2(0f, -dropY);
            }

            LastStepDropped = false;
            return new Vector2(Direction * stepX, 0f);
        }
    }
}
