using System;
using System.Collections.Generic;

namespace Pretzel.Sfx
{
    /// <summary>Remembers when each sound last played and says whether it may play again. Plain C#, so it is testable with any clock.</summary>
    public sealed class SfxThrottle
    {
        private readonly Dictionary<string, float> _lastPlayed = new(StringComparer.Ordinal);

        /// <summary>
        /// True when the sound may play now, and records the play. A cooldown of 0 or less always allows it.
        /// A refusal does not restart the cooldown.
        /// </summary>
        public bool TryAcquire(string id, float now, float cooldownSeconds)
        {
            if (cooldownSeconds > 0f && _lastPlayed.TryGetValue(id, out float last) && now - last < cooldownSeconds)
            {
                return false;
            }

            _lastPlayed[id] = now;
            return true;
        }

        public void Reset()
        {
            _lastPlayed.Clear();
        }
    }
}
