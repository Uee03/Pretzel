namespace Pretzel.Integrations.R3
{
    using System;
    using global::R3;
    using Pretzel.Core;
    using Pretzel.Events;

    /// <summary>
    /// R3 streams over ScriptableObject event channels. Subscribing to the observable subscribes to the channel, and disposing
    /// the subscription unsubscribes, so a channel (which outlives scenes) never keeps a destroyed listener.
    /// </summary>
    public static class EventChannelObservableExtensions
    {
        /// <summary>Emits the payload of every raise of the channel.</summary>
        public static Observable<T> AsObservable<T>(this EventChannel<T> channel)
        {
            Guard.NotNull(channel, nameof(channel));
            return Observable.Create<T, EventChannel<T>>(
                channel,
                static (observer, state) => state.Subscribe(observer.OnNext));
        }

        /// <summary>Emits a <see cref="Unit"/> every time the channel is raised.</summary>
        public static Observable<Unit> AsObservable(this VoidEventChannel channel)
        {
            Guard.NotNull(channel, nameof(channel));
            return Observable.Create<Unit, VoidEventChannel>(
                channel,
                static (observer, state) => state.Subscribe(() => observer.OnNext(Unit.Default)));
        }

        /// <summary>Raises the channel with every value of the stream. Dispose the subscription to stop.</summary>
        public static IDisposable RaiseOn<T>(this Observable<T> source, EventChannel<T> channel)
        {
            Guard.NotNull(channel, nameof(channel));
            return source.Subscribe(channel, static (value, state) => state.Raise(value));
        }

        /// <summary>Raises the channel every time the stream emits.</summary>
        public static IDisposable RaiseOn(this Observable<Unit> source, VoidEventChannel channel)
        {
            Guard.NotNull(channel, nameof(channel));
            return source.Subscribe(channel, static (_, state) => state.Raise());
        }
    }
}
