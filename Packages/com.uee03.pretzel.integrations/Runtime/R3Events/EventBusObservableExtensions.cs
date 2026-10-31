namespace Pretzel.Integrations.R3
{
    using System;
    using global::R3;
    using Pretzel.Core;
    using Pretzel.Events;

    /// <summary>
    /// R3 streams over an <see cref="IEventBus"/>. Subscribing to the observable subscribes to the bus, and disposing the
    /// subscription unsubscribes. The bus is main-thread only, so the streams emit on the main thread.
    /// </summary>
    public static class EventBusObservableExtensions
    {
        /// <summary>Emits every <typeparamref name="T"/> message published on the bus.</summary>
        public static Observable<T> AsObservable<T>(this IEventBus bus)
        {
            Guard.NotNull(bus, nameof(bus));
            return Observable.Create<T, IEventBus>(
                bus,
                static (observer, state) => state.Subscribe<T>(observer.OnNext));
        }

        /// <summary>Publishes every value of the stream on the bus. Dispose the subscription to stop.</summary>
        public static IDisposable PublishTo<T>(this Observable<T> source, IEventBus bus)
        {
            Guard.NotNull(bus, nameof(bus));
            return source.Subscribe(bus, static (value, state) => state.Publish(value));
        }
    }
}
