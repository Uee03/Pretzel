using System;
using Pretzel.Core;
using UnityEngine;

namespace Pretzel.Events
{
    /// <summary>A ScriptableObject event without a payload. Subscribers must unsubscribe when they are disabled.</summary>
    [CreateAssetMenu(menuName = "Pretzel/Events/Void Channel", fileName = "VoidEventChannel")]
    public sealed class VoidEventChannel : ScriptableObject
    {
        [TextArea, SerializeField] private string _description;

        [Tooltip("Logs every raise to the Console. Useful while wiring a scene.")]
        [SerializeField] private bool _logRaises;

        private Action[] _handlers = Array.Empty<Action>();

        public string Description => _description;

        public int SubscriberCount => _handlers.Length;

        public IDisposable Subscribe(Action handler)
        {
            Guard.NotNull(handler, nameof(handler));
            Action[] next = new Action[_handlers.Length + 1];
            Array.Copy(_handlers, next, _handlers.Length);
            next[_handlers.Length] = handler;
            _handlers = next;
            return new DisposableAction(() => Unsubscribe(handler));
        }

        public void Unsubscribe(Action handler)
        {
            int index = Array.IndexOf(_handlers, handler);
            if (index < 0)
            {
                return;
            }

            Action[] next = new Action[_handlers.Length - 1];
            Array.Copy(_handlers, 0, next, 0, index);
            Array.Copy(_handlers, index + 1, next, index, _handlers.Length - index - 1);
            _handlers = next;
        }

        public void Raise()
        {
            if (_logRaises)
            {
                Debug.Log($"{name} raised.", this);
            }

            Action[] snapshot = _handlers;
            for (int i = 0; i < snapshot.Length; i++)
            {
                try
                {
                    snapshot[i]();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex, this);
                }
            }
        }
    }
}
