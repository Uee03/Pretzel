using System;
using System.Collections.Generic;
using Pretzel.Core;
using UnityEngine;

namespace Pretzel.Events
{
    /// <summary>
    /// Typed in-memory message bus. Prefer small structs as messages. Handlers may subscribe or unsubscribe while a message is
    /// published; the change applies to the next publish. An exception in one handler is logged and does not stop the others.
    /// </summary>
    public sealed class EventBus : IEventBus
    {
        private readonly Dictionary<Type, object> _handlers = new();

        public IDisposable Subscribe<T>(Action<T> handler)
        {
            Guard.NotNull(handler, nameof(handler));
            HandlerList<T> list = GetList<T>(true);
            list.Add(handler);
            return new DisposableAction(() => list.Remove(handler));
        }

        public void Publish<T>(T message)
        {
            HandlerList<T> list = GetList<T>(false);
            if (list == null)
            {
                return;
            }

            Action<T>[] snapshot = list.Snapshot;
            for (int i = 0; i < snapshot.Length; i++)
            {
                try
                {
                    snapshot[i](message);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        public int SubscriberCount<T>() => GetList<T>(false)?.Snapshot.Length ?? 0;

        /// <summary>Removes every subscriber of every message type.</summary>
        public void Clear() => _handlers.Clear();

        private HandlerList<T> GetList<T>(bool create)
        {
            if (_handlers.TryGetValue(typeof(T), out object existing))
            {
                return (HandlerList<T>)existing;
            }

            if (!create)
            {
                return null;
            }

            HandlerList<T> list = new();
            _handlers.Add(typeof(T), list);
            return list;
        }

        // Copy-on-write, so publishing never allocates and never sees a half-edited list.
        private sealed class HandlerList<T>
        {
            public Action<T>[] Snapshot { get; private set; } = Array.Empty<Action<T>>();

            public void Add(Action<T> handler)
            {
                Action<T>[] next = new Action<T>[Snapshot.Length + 1];
                Array.Copy(Snapshot, next, Snapshot.Length);
                next[Snapshot.Length] = handler;
                Snapshot = next;
            }

            public void Remove(Action<T> handler)
            {
                int index = Array.IndexOf(Snapshot, handler);
                if (index < 0)
                {
                    return;
                }

                Action<T>[] next = new Action<T>[Snapshot.Length - 1];
                Array.Copy(Snapshot, 0, next, 0, index);
                Array.Copy(Snapshot, index + 1, next, index, Snapshot.Length - index - 1);
                Snapshot = next;
            }
        }
    }
}
