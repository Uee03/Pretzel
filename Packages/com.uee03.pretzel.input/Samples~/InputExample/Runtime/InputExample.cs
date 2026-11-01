using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pretzel.Inputs.Samples
{
    /// <summary>
    /// The sample's composition root: it builds the router, the device tracker and the binding store, applies saved
    /// rebinds, and switches contexts when the player pauses. Everything else in the sample asks it for what it needs.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class InputExample : MonoBehaviour
    {
        public const string GameplayContext = "Gameplay";
        public const string MenuContext = "Menu";

        [SerializeField] private InputActionAsset _actions;
        [SerializeField] private string _bindingKey = "pretzel.input.example";

        private InputAction _pause;
        private InputAction _resume;

        public InputActionAsset Actions => _actions;

        public InputRouter Router { get; private set; }

        public InputDeviceTracker Devices { get; private set; }

        public IBindingStore Store { get; private set; }

        private void Awake()
        {
            if (_actions == null)
            {
                Debug.LogError("InputExample needs the actions asset.", this);
                enabled = false;
                return;
            }

            // Work on a copy so rebinding never changes the asset on disk.
            _actions = Instantiate(_actions);
            Store = new PlayerPrefsBindingStore(_bindingKey);
            BindingPersistence.Apply(_actions, Store);

            Router = new InputRouter(_actions);
            Devices = new InputDeviceTracker();
            _pause = _actions.FindAction("Gameplay/Pause", true);
            _resume = _actions.FindAction("Menu/Resume", true);
            _pause.performed += OnPause;
            _resume.performed += OnResume;
            Router.Contexts.Push(GameplayContext);
        }

        private void OnDestroy()
        {
            if (_pause != null)
            {
                _pause.performed -= OnPause;
            }

            if (_resume != null)
            {
                _resume.performed -= OnResume;
            }

            Devices?.Dispose();
            Router?.Dispose();
            if (_actions != null)
            {
                Destroy(_actions);
            }
        }

        private void OnPause(InputAction.CallbackContext context)
        {
            Router.Contexts.Push(MenuContext);
        }

        private void OnResume(InputAction.CallbackContext context)
        {
            Router.Contexts.Remove(MenuContext);
        }
    }
}
