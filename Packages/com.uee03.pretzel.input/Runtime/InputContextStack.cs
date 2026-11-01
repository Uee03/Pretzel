using System;
using System.Collections.Generic;

namespace Pretzel.Inputs
{
    /// <summary>
    /// The input contexts of a game, as a stack: Gameplay at the bottom, a pause menu pushed on top, a dialog above that.
    /// The top one is current. Contexts are names, and each one is the name of an action map. Plain C#, so it is testable.
    /// </summary>
    public sealed class InputContextStack
    {
        private readonly List<string> _contexts = new(4);

        /// <summary>Raised after every change, with the new current context or null when the stack is empty.</summary>
        public event Action<string> Changed;

        public string Current => _contexts.Count > 0 ? _contexts[_contexts.Count - 1] : null;

        public int Count => _contexts.Count;

        public bool Contains(string context)
        {
            return IndexOf(context) >= 0;
        }

        /// <summary>Makes a context current. A context already on the stack moves to the top instead of being added twice.</summary>
        public void Push(string context)
        {
            if (string.IsNullOrEmpty(context))
            {
                throw new ArgumentException("A context needs a name.", nameof(context));
            }

            int index = IndexOf(context);
            if (index >= 0 && index == _contexts.Count - 1)
            {
                return;
            }

            if (index >= 0)
            {
                _contexts.RemoveAt(index);
            }

            _contexts.Add(context);
            Changed?.Invoke(Current);
        }

        /// <summary>Removes a context wherever it is. The one below it becomes current when it was on top. Returns false when it was not there.</summary>
        public bool Remove(string context)
        {
            int index = IndexOf(context);
            if (index < 0)
            {
                return false;
            }

            _contexts.RemoveAt(index);
            Changed?.Invoke(Current);
            return true;
        }

        public void Clear()
        {
            if (_contexts.Count > 0)
            {
                _contexts.Clear();
                Changed?.Invoke(null);
            }
        }

        private int IndexOf(string context)
        {
            for (int i = 0; i < _contexts.Count; i++)
            {
                if (string.Equals(_contexts[i], context, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
