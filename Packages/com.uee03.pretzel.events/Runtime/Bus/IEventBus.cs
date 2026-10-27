using System;

namespace Pretzel.Events
{
    /// <summary>Publishes typed messages to subscribers of the same type. Use it from the main thread.</summary>
    public interface IEventBus
    {
        /// <returns>Dispose it to unsubscribe.</returns>
        IDisposable Subscribe<T>(Action<T> handler);

        void Publish<T>(T message);

        int SubscriberCount<T>();
    }
}
