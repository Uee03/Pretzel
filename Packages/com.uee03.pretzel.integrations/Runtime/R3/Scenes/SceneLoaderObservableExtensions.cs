namespace Pretzel.Integrations.R3
{
    using System;
    using global::R3;
    using Pretzel.Scenes;

    /// <summary>
    /// Observables over a <see cref="SceneLoader"/>, for loading screens, input blocking, audio ducking and analytics.
    /// Dispose the subscription (for example with AddTo) to remove the underlying event handler.
    /// </summary>
    public static class SceneLoaderObservableExtensions
    {
        /// <summary>Emits the current state on subscribe, then every change.</summary>
        public static Observable<SceneLoadState> ObserveState(this SceneLoader loader)
        {
            return Observable.Create<SceneLoadState, SceneLoader>(
                loader,
                static (observer, state) =>
                {
                    observer.OnNext(state.State);
                    Action<SceneLoadState> handler = observer.OnNext;
                    state.StateChanged += handler;
                    return Disposable.Create((state, handler), static s => s.state.StateChanged -= s.handler);
                });
        }

        /// <summary>Emits the current progress on subscribe, then every change, from 0 to 1.</summary>
        public static Observable<float> ObserveProgress(this SceneLoader loader)
        {
            return Observable.Create<float, SceneLoader>(
                loader,
                static (observer, state) =>
                {
                    observer.OnNext(state.Progress);
                    Action<float> handler = observer.OnNext;
                    state.ProgressChanged += handler;
                    return Disposable.Create((state, handler), static s => s.state.ProgressChanged -= s.handler);
                });
        }

        /// <summary>Emits when a load starts, before its transition shows.</summary>
        public static Observable<SceneLoadReport> ObserveLoadStarted(this SceneLoader loader)
        {
            return Observable.Create<SceneLoadReport, SceneLoader>(
                loader,
                static (observer, state) =>
                {
                    Action<SceneLoadReport> handler = observer.OnNext;
                    state.LoadStarted += handler;
                    return Disposable.Create((state, handler), static s => s.state.LoadStarted -= s.handler);
                });
        }

        /// <summary>Emits the report of every finished load, successful or not.</summary>
        public static Observable<SceneLoadReport> ObserveLoadFinished(this SceneLoader loader)
        {
            return Observable.Create<SceneLoadReport, SceneLoader>(
                loader,
                static (observer, state) =>
                {
                    Action<SceneLoadReport> handler = observer.OnNext;
                    state.LoadFinished += handler;
                    return Disposable.Create((state, handler), static s => s.state.LoadFinished -= s.handler);
                });
        }

        /// <summary>True while a load or activation runs. A held preload counts as not loading, so gameplay continues.</summary>
        public static Observable<bool> ObserveIsLoading(this SceneLoader loader)
        {
            return loader.ObserveState()
                .Select(static state => state != SceneLoadState.Idle && state != SceneLoadState.Preloaded)
                .DistinctUntilChanged();
        }
    }
}
