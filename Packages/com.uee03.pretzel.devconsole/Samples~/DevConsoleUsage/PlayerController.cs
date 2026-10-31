using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Pretzel.Samples.Console
{
    /// <summary>Moves the player with WASD or the arrow keys and defeats the nearest enemy inside the aura twice a second.</summary>
    public sealed class PlayerController : MonoBehaviour
    {
        private const float AuraInterval = 0.5f;

        private ArenaGame _game;
        private float _nextAura;

        public void Initialize(ArenaGame game)
        {
            _game = game;
        }

        private void Update()
        {
            if (_game == null || _game.Player.IsDead)
            {
                return;
            }

            Vector2 input = _game.InputBlocked ? Vector2.zero : ReadMove();
            transform.position += new Vector3(input.x, 0f, input.y) * (_game.Tuning.PlayerSpeed * Time.deltaTime);

            if (Time.time >= _nextAura)
            {
                _nextAura = Time.time + AuraInterval;
                Enemy nearest = _game.FindNearestEnemy(transform.position, _game.Tuning.AuraRadius);
                if (nearest)
                {
                    _game.Defeat(nearest);
                }
            }
        }

        private static Vector2 ReadMove()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            float y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
#else
            return Vector2.zero;
#endif
        }
    }
}
