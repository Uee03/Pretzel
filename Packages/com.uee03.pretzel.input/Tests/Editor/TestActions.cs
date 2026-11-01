using UnityEngine;
using UnityEngine.InputSystem;

namespace Pretzel.Inputs.Tests
{
    /// <summary>Builds the action asset the Input System tests use: Gameplay, Menu and Debug maps with one keyboard binding each, and a gamepad one on Jump.</summary>
    internal static class TestActions
    {
        public static InputActionAsset Create()
        {
            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();

            InputActionMap gameplay = asset.AddActionMap("Gameplay");
            InputAction jump = gameplay.AddAction("Jump", InputActionType.Button);
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            InputActionMap menu = asset.AddActionMap("Menu");
            menu.AddAction("Submit", InputActionType.Button).AddBinding("<Keyboard>/enter");

            InputActionMap debug = asset.AddActionMap("Debug");
            debug.AddAction("Toggle", InputActionType.Button).AddBinding("<Keyboard>/f1");

            return asset;
        }
    }
}
