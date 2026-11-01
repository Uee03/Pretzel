using System;
using Pretzel.Inputs;
using Pretzel.Inputs.Scenes;
using Pretzel.Scenes;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace SpaceInvaders
{
    /// <summary>
    /// The game's input: one router over the project-wide actions. The Player map is live while playing, the UI map is always
    /// live so menus and the pause screen can be used, and all of it is blocked while a scene loads. Pausing is a context
    /// change, so gameplay code has no "if paused" checks.
    /// </summary>
    public sealed class GameInput : IStartable, IDisposable
    {
        public const string PlayerContext = "Player";
        public const string PausedContext = "Paused";
        public const string AlwaysOnMap = "UI";

        private readonly SceneLoader _loader;
        private SceneLoadInputGate _gate;

        public GameInput(SceneLoader loader)
        {
            _loader = loader;
        }

        public InputRouter Router { get; private set; }

        public InputDeviceTracker Devices { get; private set; }

        /// <summary>The device the player used last, for prompts.</summary>
        public InputDeviceKind LastDevice => Devices?.Kind ?? InputDeviceKind.Unknown;

        public void Start()
        {
            InputActionAsset actions = InputSystem.actions
                ?? throw new InvalidOperationException("No project-wide input actions are assigned. Set them in Project Settings > Input System Package.");
            Router = new InputRouter(actions, AlwaysOnMap);
            Router.Contexts.Push(PlayerContext);
            Devices = new InputDeviceTracker();
            _gate = new SceneLoadInputGate(_loader, Router);
        }

        /// <summary>Switches off the Player map and keeps the UI map. A no-op before the game input has started.</summary>
        public void Pause()
        {
            Router?.Contexts.Push(PausedContext);
        }

        /// <summary>Brings the Player map back. Safe to call when not paused.</summary>
        public void Resume()
        {
            Router?.Contexts.Remove(PausedContext);
        }

        public void Dispose()
        {
            _gate?.Dispose();
            Devices?.Dispose();
            Router?.Dispose();
        }
    }
}
