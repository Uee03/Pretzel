using UnityEngine;

namespace Pretzel.Inputs
{
    /// <summary>
    /// Remembers a button press for a short window, so a press that arrives just before the game can use it, such as a
    /// jump pressed a few frames before landing, still counts. Plain C#: pass in the time.
    /// </summary>
    public sealed class InputBuffer
    {
        private float _pressedAt = float.NegativeInfinity;

        public InputBuffer(float windowSeconds = 0.12f)
        {
            WindowSeconds = Mathf.Max(0f, windowSeconds);
        }

        public float WindowSeconds { get; set; }

        /// <summary>Records a press at the given time.</summary>
        public void Press(float now)
        {
            _pressedAt = now;
        }

        /// <summary>True when a press is still inside the window. The press stays buffered.</summary>
        public bool Peek(float now)
        {
            return now - _pressedAt <= WindowSeconds;
        }

        /// <summary>True when a press is inside the window, and uses it up so it counts once.</summary>
        public bool Consume(float now)
        {
            if (!Peek(now))
            {
                return false;
            }

            _pressedAt = float.NegativeInfinity;
            return true;
        }

        public void Clear()
        {
            _pressedAt = float.NegativeInfinity;
        }
    }
}
