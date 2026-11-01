using System;
using UnityEngine.InputSystem;

namespace Pretzel.Inputs
{
    /// <summary>
    /// Enables the action map of the current context and disables every other, and disables all of them while input is
    /// blocked. Name your action maps after your contexts (Gameplay, Menu, Dialog) and push and pop contexts from game
    /// code. It changes a map only when its state differs, so a refresh costs nothing when nothing changed.
    /// </summary>
    public sealed class InputRouter : IDisposable
    {
        private readonly InputActionAsset _asset;
        private readonly string _alwaysOnMap;
        private bool _disposed;

        /// <param name="alwaysOnMap">An action map that stays enabled in every context, unless input is blocked, such as a debug map. Optional.</param>
        public InputRouter(InputActionAsset asset, string alwaysOnMap = null)
        {
            _asset = asset ?? throw new ArgumentNullException(nameof(asset));
            _alwaysOnMap = alwaysOnMap;
            Contexts = new InputContextStack();
            Blockers = new InputBlockers();
            Contexts.Changed += OnContextChanged;
            Blockers.Changed += OnBlockedChanged;
            Refresh();
        }

        public InputContextStack Contexts { get; }

        public InputBlockers Blockers { get; }

        /// <summary>The context whose map is enabled right now, or null when blocked or empty.</summary>
        public string ActiveContext => Blockers.IsBlocked ? null : Contexts.Current;

        /// <summary>Applies the stack and the blockers to the action maps.</summary>
        public void Refresh()
        {
            if (_disposed)
            {
                return;
            }

            string current = Contexts.Current;
            bool blocked = Blockers.IsBlocked;
            foreach (InputActionMap map in _asset.actionMaps)
            {
                bool wanted = !blocked
                    && (string.Equals(map.name, current, StringComparison.Ordinal)
                        || (_alwaysOnMap != null && string.Equals(map.name, _alwaysOnMap, StringComparison.Ordinal)));
                if (wanted && !map.enabled)
                {
                    map.Enable();
                }
                else if (!wanted && map.enabled)
                {
                    map.Disable();
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Contexts.Changed -= OnContextChanged;
            Blockers.Changed -= OnBlockedChanged;
            _disposed = true;
            _asset.Disable();
        }

        private void OnContextChanged(string context)
        {
            Refresh();
        }

        private void OnBlockedChanged(bool blocked)
        {
            Refresh();
        }
    }
}
