using System;
using UnityEngine.InputSystem;

namespace Pretzel.Inputs
{
    /// <summary>
    /// One interactive rebind of a binding: wait for the player to press a control, then change the binding. It disables
    /// the action while it waits (the Input System requires that) and enables it again afterwards, ignores mouse
    /// movement, lets Escape cancel, and always disposes the underlying operation, including when it is disposed early.
    /// </summary>
    public sealed class RebindSession : IDisposable
    {
        private readonly InputAction _action;
        private readonly bool _wasEnabled;
        private InputActionRebindingExtensions.RebindingOperation _operation;
        private Action<RebindResult> _completed;

        private RebindSession(InputAction action)
        {
            _action = action;
            _wasEnabled = action.enabled;
        }

        public bool IsRunning => _operation != null;

        /// <summary>Starts a rebind of one binding of an action. <paramref name="completed"/> is called once, when it ends.</summary>
        public static RebindSession Start(InputAction action, int bindingIndex, Action<RebindResult> completed, string cancelPath = "<Keyboard>/escape")
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(bindingIndex), "The action has no binding at that index.");
            }

            RebindSession session = new(action) { _completed = completed };
            if (session._wasEnabled)
            {
                action.Disable();
            }

            InputActionRebindingExtensions.RebindingOperation operation = action.PerformInteractiveRebinding(bindingIndex)
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithControlsExcluding("<Pointer>/position")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(_ => session.Finish(RebindResult.Completed))
                .OnCancel(_ => session.Finish(RebindResult.Cancelled));
            if (!string.IsNullOrEmpty(cancelPath))
            {
                operation.WithCancelingThrough(cancelPath);
            }

            session._operation = operation;
            operation.Start();
            return session;
        }

        /// <summary>Cancels a rebind that is still waiting.</summary>
        public void Cancel()
        {
            _operation?.Cancel();
        }

        public void Dispose()
        {
            if (_operation != null)
            {
                _completed = null;
                _operation.Cancel();
                Release();
            }
        }

        private void Finish(RebindResult result)
        {
            Action<RebindResult> completed = _completed;
            _completed = null;
            Release();
            completed?.Invoke(result);
        }

        private void Release()
        {
            if (_operation != null)
            {
                _operation.Dispose();
                _operation = null;
                if (_wasEnabled)
                {
                    _action.Enable();
                }
            }
        }
    }
}
