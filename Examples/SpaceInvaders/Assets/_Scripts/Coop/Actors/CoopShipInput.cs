using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceInvaders.Coop
{
    /// <summary>
    /// Keyboard and gamepad schemes for couch co-op. Player 1: A / D and Space. Player 2: the arrow keys and Enter. Each local player also
    /// gets the gamepad with their index. A machine with one player accepts every scheme, which is how an online player steers.
    /// </summary>
    public sealed class CoopShipInput : ICoopShipInput
    {
        public float ReadHorizontal(int localPlayerIndex, bool acceptEveryScheme)
        {
            float value = 0f;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (acceptEveryScheme || localPlayerIndex == 0)
                {
                    value += Axis(keyboard.dKey.isPressed, keyboard.aKey.isPressed);
                }

                if (acceptEveryScheme || localPlayerIndex == 1)
                {
                    value += Axis(keyboard.rightArrowKey.isPressed, keyboard.leftArrowKey.isPressed);
                }
            }

            Gamepad pad = PadFor(localPlayerIndex, acceptEveryScheme);
            if (pad != null)
            {
                value += pad.leftStick.x.ReadValue();
                value += Axis(pad.dpad.right.isPressed, pad.dpad.left.isPressed);
            }

            return Mathf.Clamp(value, -1f, 1f);
        }

        public bool ReadFire(int localPlayerIndex, bool acceptEveryScheme)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if ((acceptEveryScheme || localPlayerIndex == 0) && keyboard.spaceKey.isPressed)
                {
                    return true;
                }

                if ((acceptEveryScheme || localPlayerIndex == 1) && (keyboard.enterKey.isPressed || keyboard.rightCtrlKey.isPressed))
                {
                    return true;
                }
            }

            Gamepad pad = PadFor(localPlayerIndex, acceptEveryScheme);
            return pad != null && (pad.buttonSouth.isPressed || pad.buttonWest.isPressed);
        }

        private static float Axis(bool positive, bool negative)
        {
            return (positive ? 1f : 0f) - (negative ? 1f : 0f);
        }

        private static Gamepad PadFor(int localPlayerIndex, bool acceptEveryScheme)
        {
            if (acceptEveryScheme)
            {
                return Gamepad.current;
            }

            return localPlayerIndex < Gamepad.all.Count ? Gamepad.all[localPlayerIndex] : null;
        }
    }
}
