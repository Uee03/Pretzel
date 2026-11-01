using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceInvaders.Gameplay
{
    /// <summary>Reads the project-wide Input System actions, so rebinding or a new device needs no code change.</summary>
    public sealed class ShipControls
    {
        private readonly InputAction _move;
        private readonly InputAction _attack;
        private readonly InputAction _jump;
        private readonly InputAction _cancel;

        public ShipControls()
        {
            InputActionAsset actions = InputSystem.actions;
            if (actions == null)
            {
                throw new InvalidOperationException("No project-wide input actions are assigned. Set them in Project Settings > Input System Package.");
            }

            _move = actions.FindAction("Player/Move", true);
            _attack = actions.FindAction("Player/Attack", true);
            _jump = actions.FindAction("Player/Jump", true);
            _cancel = actions.FindAction("UI/Cancel", true);
            if (!actions.enabled)
            {
                actions.Enable();
            }
        }

        public float Horizontal => _move.ReadValue<Vector2>().x;

        // Space (Jump) and the Attack bindings both fire, so keyboard, mouse and gamepad players are covered.
        public bool FireHeld => _attack.IsPressed() || _jump.IsPressed();

        public bool PausePressed => _cancel.WasPressedThisFrame();
    }
}
