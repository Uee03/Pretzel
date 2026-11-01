using System;
using UnityEngine.InputSystem;

namespace Pretzel.Inputs
{
    /// <summary>
    /// Tracks which kind of device the player used last, so button prompts and menus can follow it. It listens to the
    /// actions that fire, so it needs no polling, and it raises an event only when the kind actually changes.
    /// </summary>
    public sealed class InputDeviceTracker : IDisposable
    {
        private bool _subscribed;

        public InputDeviceTracker()
        {
            InputSystem.onActionChange += OnActionChange;
            _subscribed = true;
        }

        public InputDeviceKind Kind { get; private set; }

        public event Action<InputDeviceKind> Changed;

        /// <summary>Classifies a device. Mouse and keyboard are one kind, since players switch between them freely.</summary>
        public static InputDeviceKind KindOf(InputDevice device)
        {
            switch (device)
            {
                case Keyboard:
                case Mouse:
                    return InputDeviceKind.KeyboardMouse;
                case Gamepad:
                    return InputDeviceKind.Gamepad;
                case Touchscreen:
                    return InputDeviceKind.Touch;
                default:
                    return InputDeviceKind.Unknown;
            }
        }

        /// <summary>Records that a device was used. Called for you by the action listener; call it yourself for custom input.</summary>
        public void Observe(InputDevice device)
        {
            InputDeviceKind kind = KindOf(device);
            if (kind != InputDeviceKind.Unknown && kind != Kind)
            {
                Kind = kind;
                Changed?.Invoke(kind);
            }
        }

        public void Dispose()
        {
            if (_subscribed)
            {
                InputSystem.onActionChange -= OnActionChange;
                _subscribed = false;
            }
        }

        private void OnActionChange(object source, InputActionChange change)
        {
            if (change == InputActionChange.ActionPerformed && source is InputAction action && action.activeControl != null)
            {
                Observe(action.activeControl.device);
            }
        }
    }
}
