using UnityEngine;
#if PRETZEL_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Pretzel.Multiplayer.Samples.TwoCubes
{
    /// <summary>
    /// Keyboard schemes for couch co-op. Local player 0: WASD and Space. Local player 1: arrow keys and Right Ctrl or Enter.
    /// Other local players have no keys. Uses the Input System when it is installed and active, otherwise the legacy Input Manager.
    /// </summary>
    public sealed class TwoCubesKeyboardInput : ITwoCubesInput
    {
        public Vector2 ReadMove(int localPlayerIndex)
        {
#if PRETZEL_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            if (localPlayerIndex == 0)
            {
                return new Vector2(Axis(keyboard.dKey, keyboard.aKey), Axis(keyboard.wKey, keyboard.sKey));
            }

            if (localPlayerIndex == 1)
            {
                return new Vector2(Axis(keyboard.rightArrowKey, keyboard.leftArrowKey), Axis(keyboard.upArrowKey, keyboard.downArrowKey));
            }

            return Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (localPlayerIndex == 0)
            {
                return new Vector2(Axis(KeyCode.D, KeyCode.A), Axis(KeyCode.W, KeyCode.S));
            }

            if (localPlayerIndex == 1)
            {
                return new Vector2(Axis(KeyCode.RightArrow, KeyCode.LeftArrow), Axis(KeyCode.UpArrow, KeyCode.DownArrow));
            }

            return Vector2.zero;
#else
            return Vector2.zero;
#endif
        }

        public bool ReadJump(int localPlayerIndex)
        {
#if PRETZEL_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            if (localPlayerIndex == 0)
            {
                return keyboard.spaceKey.isPressed;
            }

            return localPlayerIndex == 1 && (keyboard.rightCtrlKey.isPressed || keyboard.enterKey.isPressed || keyboard.numpadEnterKey.isPressed);
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (localPlayerIndex == 0)
            {
                return Input.GetKey(KeyCode.Space);
            }

            return localPlayerIndex == 1 && (Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter));
#else
            return false;
#endif
        }

#if PRETZEL_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
        private static float Axis(UnityEngine.InputSystem.Controls.KeyControl positive, UnityEngine.InputSystem.Controls.KeyControl negative)
        {
            return (positive.isPressed ? 1f : 0f) - (negative.isPressed ? 1f : 0f);
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        private static float Axis(KeyCode positive, KeyCode negative)
        {
            return (Input.GetKey(positive) ? 1f : 0f) - (Input.GetKey(negative) ? 1f : 0f);
        }
#endif
    }
}
