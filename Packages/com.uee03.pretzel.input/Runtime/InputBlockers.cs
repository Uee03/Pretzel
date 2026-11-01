using System;
using System.Collections.Generic;

namespace Pretzel.Inputs
{
    /// <summary>
    /// Reasons why input is off, such as a scene load or a cutscene. Input is blocked while any reason is held, and each
    /// reason is a name, so a duplicate or a missing release cannot leave input stuck or turn it on too early.
    /// </summary>
    public sealed class InputBlockers
    {
        private readonly HashSet<string> _reasons = new(StringComparer.Ordinal);

        /// <summary>Raised when input becomes blocked or unblocked, with the new state.</summary>
        public event Action<bool> Changed;

        public bool IsBlocked => _reasons.Count > 0;

        public int Count => _reasons.Count;

        public bool Has(string reason)
        {
            return _reasons.Contains(reason);
        }

        /// <summary>Holds input off for a reason. Holding the same reason twice counts once.</summary>
        public void Block(string reason)
        {
            if (string.IsNullOrEmpty(reason))
            {
                throw new ArgumentException("A blocker needs a reason.", nameof(reason));
            }

            bool wasBlocked = IsBlocked;
            if (_reasons.Add(reason) && !wasBlocked)
            {
                Changed?.Invoke(true);
            }
        }

        /// <summary>Releases a reason. Releasing one that is not held does nothing.</summary>
        public void Unblock(string reason)
        {
            if (reason != null && _reasons.Remove(reason) && !IsBlocked)
            {
                Changed?.Invoke(false);
            }
        }

        public void Clear()
        {
            if (_reasons.Count > 0)
            {
                _reasons.Clear();
                Changed?.Invoke(false);
            }
        }
    }
}
