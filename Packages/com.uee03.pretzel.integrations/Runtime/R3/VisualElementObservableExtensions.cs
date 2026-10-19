namespace Pretzel.Integrations.R3
{
    using global::R3;
    using UnityEngine.UIElements;

    public static class VisualElementObservableExtensions
    {
        /// <summary>Emits every <typeparamref name="TEvent"/> the element receives. Disposing the subscription unregisters the callback.</summary>
        public static Observable<TEvent> EventAsObservable<TEvent>(this VisualElement element, TrickleDown trickleDown = TrickleDown.NoTrickleDown)
            where TEvent : EventBase<TEvent>, new()
        {
            return Observable.Create<TEvent, (VisualElement Element, TrickleDown Trickle)>(
                (element, trickleDown),
                static (observer, state) =>
                {
                    EventCallback<TEvent> callback = evt => observer.OnNext(evt);
                    state.Element.RegisterCallback(callback, state.Trickle);
                    return Disposable.Create((state.Element, callback, state.Trickle), static s => s.Element.UnregisterCallback(s.callback, s.Trickle));
                });
        }

        /// <summary>Emits when the button is clicked.</summary>
        public static Observable<Unit> ClickedAsObservable(this Button button)
        {
            return Observable.Create<Unit, Button>(
                button,
                static (observer, state) =>
                {
                    System.Action handler = () => observer.OnNext(Unit.Default);
                    state.clicked += handler;
                    return Disposable.Create((state, handler), static s => s.state.clicked -= s.handler);
                });
        }
    }
}
