using System;
using Pretzel.Core;
using UnityEngine;

namespace Pretzel.Events
{
    /// <summary>
    /// A ScriptableObject event with a payload, shared by reference between scenes and prefabs.
    /// Subscribers must unsubscribe when they are disabled; the asset outlives them.
    /// </summary>
    public abstract class EventChannel<T> : ScriptableObject
    {
        [TextArea, SerializeField] private string _description;

        [Tooltip("Logs every raise to the Console. Useful while wiring a scene.")]
        [SerializeField] private bool _logRaises;

        private Action<T>[] _handlers = Array.Empty<Action<T>>();

        public string Description => _description;

        public int SubscriberCount => _handlers.Length;

        public IDisposable Subscribe(Action<T> handler)
        {
            Guard.NotNull(handler, nameof(handler));
            Action<T>[] next = new Action<T>[_handlers.Length + 1];
            Array.Copy(_handlers, next, _handlers.Length);
            next[_handlers.Length] = handler;
            _handlers = next;
            return new DisposableAction(() => Unsubscribe(handler));
        }

        public void Unsubscribe(Action<T> handler)
        {
            int index = Array.IndexOf(_handlers, handler);
            if (index < 0)
            {
                return;
            }

            Action<T>[] next = new Action<T>[_handlers.Length - 1];
            Array.Copy(_handlers, 0, next, 0, index);
            Array.Copy(_handlers, index + 1, next, index, _handlers.Length - index - 1);
            _handlers = next;
        }

        public void Raise(T value)
        {
            if (_logRaises)
            {
                Debug.Log($"{name} raised: {value}", this);
            }

            Action<T>[] snapshot = _handlers;
            for (int i = 0; i < snapshot.Length; i++)
            {
                try
                {
                    snapshot[i](value);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex, this);
                }
            }
        }
    }
}
