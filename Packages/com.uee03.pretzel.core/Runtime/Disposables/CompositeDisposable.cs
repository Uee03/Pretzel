using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pretzel.Core
{
    /// <summary>
    /// Owns a group of subscriptions and disposes them together, in reverse order. Add each <see cref="IDisposable"/> a
    /// subscribe call returns, and dispose the bag once, in <c>OnDestroy</c> or the owner's <c>Dispose</c>.
    /// A failing <see cref="IDisposable.Dispose"/> is logged and the others still run. Use it from the main thread.
    /// </summary>
    public sealed class CompositeDisposable : IDisposable
    {
        private readonly List<IDisposable> _items = new();

        public int Count => _items.Count;

        /// <summary>True after <see cref="Dispose"/>. An item added afterwards is disposed at once.</summary>
        public bool IsDisposed { get; private set; }

        /// <returns>The same item, so a call can be added and still used.</returns>
        public T Add<T>(T item) where T : IDisposable
        {
            Guard.NotNull((IDisposable)item, nameof(item));
            if (IsDisposed)
            {
                Release(item);
            }
            else
            {
                _items.Add(item);
            }

            return item;
        }

        /// <summary>Disposes every item and keeps the bag usable, for an owner that subscribes again, for example on enable.</summary>
        public void Clear()
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                IDisposable item = _items[i];
                _items.RemoveAt(i);
                Release(item);
            }
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;
            Clear();
        }

        private static void Release(IDisposable item)
        {
            try
            {
                item.Dispose();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
