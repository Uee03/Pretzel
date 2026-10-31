using UnityEngine;

namespace Pretzel.Multiplayer.Samples.TwoCubes
{
    /// <summary>
    /// The cube's movement rules without any Unity component or networking: move, jump and turn. Pure arithmetic, so it is easy to test and does not allocate.
    /// 3D moves on X/Z and turns to face the movement direction; 2D moves on X and turns around when the direction flips.
    /// </summary>
    public sealed class TwoCubesMotor
    {
        public float MoveSpeed { get; set; } = 5f;

        public float TurnSpeedDegrees { get; set; } = 720f;

        public float JumpSpeed { get; set; } = 7f;

        public float Gravity { get; set; } = 20f;

        public float GroundY { get; set; } = 0.5f;

        /// <summary>The arena is a square of this half extent around the origin.</summary>
        public float ArenaHalfExtent { get; set; } = 9f;

        public Vector3 Position { get; set; }

        public float Yaw { get; set; }

        public float VerticalVelocity { get; private set; }

        public bool IsGrounded => Position.y <= GroundY + 0.0001f && VerticalVelocity <= 0f;

        public void SetState(Vector3 position, float yaw)
        {
            Position = position;
            Yaw = yaw;
            VerticalVelocity = 0f;
        }

        /// <summary>True when something changed, so callers can skip writing an unchanged transform.</summary>
        public bool Step(TwoCubesPlane plane, Vector2 move, bool jump, float deltaTime)
        {
            bool moving = move.sqrMagnitude > 0.0001f;
            bool airborne = !IsGrounded;
            if (!moving && !jump && !airborne)
            {
                return false;
            }

            Vector3 position = Position;
            if (moving)
            {
                if (plane == TwoCubesPlane.Horizontal3D)
                {
                    Vector2 direction = move.sqrMagnitude > 1f ? move.normalized : move;
                    position.x += direction.x * MoveSpeed * deltaTime;
                    position.z += direction.y * MoveSpeed * deltaTime;
                    Yaw = Mathf.MoveTowardsAngle(Yaw, Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg, TurnSpeedDegrees * deltaTime);
                }
                else
                {
                    float x = Mathf.Clamp(move.x, -1f, 1f);
                    position.x += x * MoveSpeed * deltaTime;
                    if (Mathf.Abs(x) > 0.01f)
                    {
                        // Facing right is yaw 0 and facing left is yaw 180, so the sprite visibly flips when the direction changes.
                        Yaw = Mathf.MoveTowardsAngle(Yaw, x > 0f ? 0f : 180f, TurnSpeedDegrees * deltaTime);
                    }
                }

                position.x = Mathf.Clamp(position.x, -ArenaHalfExtent, ArenaHalfExtent);
                position.z = Mathf.Clamp(position.z, -ArenaHalfExtent, ArenaHalfExtent);
            }

            if (jump && IsGrounded)
            {
                VerticalVelocity = JumpSpeed;
            }

            if (VerticalVelocity > 0f || position.y > GroundY)
            {
                VerticalVelocity -= Gravity * deltaTime;
                position.y += VerticalVelocity * deltaTime;
                if (position.y <= GroundY)
                {
                    position.y = GroundY;
                    VerticalVelocity = 0f;
                }
            }

            Position = position;
            return true;
        }
    }
}
