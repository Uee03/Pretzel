using System;
using System.Collections.Generic;
using System.Threading;
using Pretzel.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using ThreadPriority = UnityEngine.ThreadPriority;

namespace Pretzel.Scenes
{
    /// <summary>
    /// Loads scenes and scene groups behind an optional transition, one request at a time.
    /// A load shows the transition, reads the scene files with activation held back, activates them, runs
    /// <see cref="SceneLoadOptions.SetupAsync"/>, waits a few frames and hides the transition. One progress value covers it all.
    /// Requests that arrive during a load follow <see cref="RequestPolicy"/>. Use it from the main thread.
    /// Unity cannot abort a scene load, so cancelling after loading started unloads additive scenes again and lets a
    /// single-mode load finish.
    /// </summary>
    public sealed class SceneLoader
    {
        private readonly Queue<AwaitableCompletionSource> _waiting = new();
        private bool _gateHeld;
        private PreloadedScene _preloaded;

        public SceneRequestPolicy RequestPolicy { get; set; } = SceneRequestPolicy.Queue;

        public SceneLoadState State { get; private set; }

        /// <summary>True while a load, an activation or a held preload owns the loader.</summary>
        public bool IsBusy => _gateHeld;

        /// <summary>Progress of the current load, 0 to 1.</summary>
        public float Progress { get; private set; }

        public SceneLoadReport LastReport { get; private set; }

        public event Action<SceneLoadState> StateChanged;

        public event Action<float> ProgressChanged;

        /// <summary>Raised when a load starts, before the transition shows. Disable gameplay input or duck audio here.</summary>
        public event Action<SceneLoadReport> LoadStarted;

        /// <summary>Raised after every load, successful or not, once the transition is hidden. Restore input here.</summary>
        public event Action<SceneLoadReport> LoadFinished;

        /// <summary>Raised for each scene a successful load opened.</summary>
        public event Action<Scene> SceneLoaded;

        #region LOAD

        /// <returns>The loaded scene, or an invalid scene when <see cref="SceneRequestPolicy.Ignore"/> skipped the request.</returns>
        public async Awaitable<Scene> LoadAsync(string sceneName, SceneLoadOptions options = null, CancellationToken cancellationToken = default)
        {
            Guard.NotNullOrEmpty(sceneName, nameof(sceneName));
            IReadOnlyList<Scene> scenes = await RunAsync(new[] { SceneEntry.ByName(sceneName) }, sceneName, options, null, cancellationToken);
            return scenes.Count > 0 ? scenes[0] : default;
        }

        public async Awaitable<Scene> LoadAsync(int buildIndex, SceneLoadOptions options = null, CancellationToken cancellationToken = default)
        {
            Guard.InRange(buildIndex, 0, int.MaxValue, nameof(buildIndex));
            IReadOnlyList<Scene> scenes = await RunAsync(new[] { SceneEntry.ByIndex(buildIndex) }, null, options, null, cancellationToken);
            return scenes.Count > 0 ? scenes[0] : default;
        }

        /// <summary>
        /// Loads every scene of the group in order and makes <see cref="SceneGroup.ActiveScene"/> active. With a single-mode
        /// load the first scene replaces the open scenes and the rest load additively.
        /// </summary>
        /// <returns>The opened scenes in load order, or an empty list when the request was ignored.</returns>
        public Awaitable<IReadOnlyList<Scene>> LoadGroupAsync(SceneGroup group, SceneLoadOptions options = null, CancellationToken cancellationToken = default)
        {
            Guard.NotDestroyed(group, nameof(group));
            if (group.Scenes.Count == 0)
            {
                throw new ArgumentException($"Scene group '{group.name}' has no scenes.", nameof(group));
            }

            SceneEntry[] entries = new SceneEntry[group.Scenes.Count];
            for (int i = 0; i < entries.Length; i++)
            {
                entries[i] = SceneEntry.ByName(Guard.NotNullOrEmpty(group.Scenes[i], nameof(group)));
            }

            return RunAsync(entries, group.ActiveScene, options, null, cancellationToken);
        }

        /// <summary>
        /// Loads an additive scene in the background up to activation and returns when it is ready, without a transition.
        /// The loader stays busy until the returned scene is activated or cancelled.
        /// </summary>
        /// <param name="loadingPriority">Low keeps gameplay smooth while the scene streams in.</param>
        /// <returns>The held scene, or null when <see cref="SceneRequestPolicy.Ignore"/> skipped the request.</returns>
        public async Awaitable<PreloadedScene> PreloadAsync(string sceneName, IProgress<float> progress = null,
            ThreadPriority loadingPriority = ThreadPriority.Low, CancellationToken cancellationToken = default)
        {
            Guard.NotNullOrEmpty(sceneName, nameof(sceneName));
            if (!await AcquireAsync(cancellationToken))
            {
                return null;
            }

            ThreadPriority original = Application.backgroundLoadingPriority;
            PreloadedScene handle = null;
            try
            {
                ResetProgress();
                SetState(SceneLoadState.Loading);
                Application.backgroundLoadingPriority = loadingPriority;
                AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                if (operation == null)
                {
                    throw MissingScene(sceneName);
                }

                operation.allowSceneActivation = false;
                handle = new PreloadedScene(this, sceneName, operation, original);
                _preloaded = handle;
                while (operation.progress < 0.9f && !cancellationToken.IsCancellationRequested)
                {
                    ReportBoth(progress, operation.progress / 0.9f);
                    await Awaitable.NextFrameAsync();
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    await DiscardPreloadAsync(handle);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                ReportBoth(progress, 1f);
                SetState(SceneLoadState.Preloaded);
                return handle;
            }
            catch when (handle == null)
            {
                Application.backgroundLoadingPriority = original;
                SetState(SceneLoadState.Idle);
                Release();
                throw;
            }
        }

        internal Awaitable<Scene> ActivatePreloadAsync(PreloadedScene handle, SceneLoadOptions options, CancellationToken cancellationToken)
        {
            ThrowIfNotHeld(handle);
            handle.IsConsumed = true;
            return ActivateHeldAsync(handle, options, cancellationToken);
        }

        internal Awaitable CancelPreloadAsync(PreloadedScene handle)
        {
            ThrowIfNotHeld(handle);
            return DiscardPreloadAsync(handle);
        }

        private async Awaitable DiscardPreloadAsync(PreloadedScene handle)
        {
            handle.IsConsumed = true;
            try
            {
                SetState(SceneLoadState.Activating);
                Scene scene = await ActivateOperationAsync(handle.Operation);
                if (scene.IsValid() && scene.isLoaded)
                {
                    await UnloadAsync(scene);
                }
            }
            finally
            {
                Application.backgroundLoadingPriority = handle.OriginalPriority;
                _preloaded = null;
                SetState(SceneLoadState.Idle);
                Release();
            }
        }

        private async Awaitable<Scene> ActivateHeldAsync(PreloadedScene handle, SceneLoadOptions options, CancellationToken cancellationToken)
        {
            IReadOnlyList<Scene> scenes = await RunAsync(new[] { SceneEntry.ByName(handle.SceneName) }, handle.SceneName, options, handle, cancellationToken);
            return scenes.Count > 0 ? scenes[0] : default;
        }

        #endregion

        #region UNLOAD

        public Awaitable UnloadAsync(string sceneName, CancellationToken cancellationToken = default)
        {
            Guard.NotNullOrEmpty(sceneName, nameof(sceneName));
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException($"Scene '{sceneName}' is not loaded.");
            }

            return UnloadAsync(scene, cancellationToken);
        }

        /// <summary>Unity cannot cancel an unload; the token only stops the wait.</summary>
        public async Awaitable UnloadAsync(Scene scene, CancellationToken cancellationToken = default)
        {
            AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);
            if (operation == null)
            {
                throw new InvalidOperationException($"Scene '{scene.name}' cannot be unloaded. The last loaded scene cannot be unloaded.");
            }

            while (!operation.isDone)
            {
                await Awaitable.NextFrameAsync(cancellationToken);
            }
        }

        /// <summary>Unloads every scene of the group that is loaded. Scenes that are not loaded are skipped.</summary>
        public async Awaitable UnloadGroupAsync(SceneGroup group, CancellationToken cancellationToken = default)
        {
            Guard.NotDestroyed(group, nameof(group));
            for (int i = group.Scenes.Count - 1; i >= 0; i--)
            {
                Scene scene = SceneManager.GetSceneByName(group.Scenes[i]);
                if (scene.IsValid() && scene.isLoaded)
                {
                    await UnloadAsync(scene, cancellationToken);
                }
            }
        }

        #endregion

        #region PIPELINE

        private async Awaitable<IReadOnlyList<Scene>> RunAsync(IReadOnlyList<SceneEntry> entries, string activeName, SceneLoadOptions options,
            PreloadedScene preloaded, CancellationToken cancellationToken)
        {
            options ??= new SceneLoadOptions();
            if (preloaded == null && !await AcquireAsync(cancellationToken))
            {
                return Array.Empty<Scene>();
            }

            LoadSceneMode mode = preloaded != null ? LoadSceneMode.Additive : options.Mode;
            string[] names = new string[entries.Count];
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = entries[i].Name;
            }

            SceneLoadReport report = new(names, mode, preloaded != null);
            double started = Now;
            ThreadPriority restorePriority = preloaded?.OriginalPriority ?? Application.backgroundLoadingPriority;
            if (options.LoadingPriority.HasValue)
            {
                Application.backgroundLoadingPriority = options.LoadingPriority.Value;
            }

            ISceneTransition transition = options.Transition;
            ISceneStatusDisplay status = transition as ISceneStatusDisplay;
            OverallProgress overall = new(options.Progress, transition, SetProgress);
            bool hasSetup = options.SetupAsync != null;
            float sceneShare = hasSetup ? Mathf.Clamp01(options.SceneProgressWeight) : 1f;
            bool additive = mode == LoadSceneMode.Additive;
            bool shown = false;
            List<Scene> loaded = new();

            ResetProgress();
            Invoke(LoadStarted, report);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (transition != null)
                {
                    SetState(SceneLoadState.ShowingTransition);
                    if (options.LoadingStatus != null)
                    {
                        status?.ReportStatus(options.LoadingStatus);
                    }

                    shown = true;
                    double showStart = Now;
                    await transition.ShowAsync(cancellationToken);
                    report.ShowSeconds = Now - showStart;
                }

                cancellationToken.ThrowIfCancellationRequested();
                double loadStart = Now;
                for (int i = 0; i < entries.Count && !cancellationToken.IsCancellationRequested; i++)
                {
                    SubProgress sceneProgress = new(overall, sceneShare * i / entries.Count, sceneShare * (i + 1) / entries.Count);
                    double phaseStart = Now;
                    AsyncOperation operation;
                    if (i == 0 && preloaded != null)
                    {
                        operation = preloaded.Operation;
                    }
                    else
                    {
                        SetState(SceneLoadState.Loading);
                        operation = entries[i].Begin(i == 0 ? mode : LoadSceneMode.Additive);
                        if (operation == null)
                        {
                            throw MissingScene(entries[i].Name);
                        }

                        operation.allowSceneActivation = false;
                    }

                    while (operation.progress < 0.9f)
                    {
                        sceneProgress.Report(operation.progress / 0.9f);
                        await Awaitable.NextFrameAsync();
                    }

                    sceneProgress.Report(1f);
                    if (i == entries.Count - 1)
                    {
                        while (Now - loadStart < options.MinimumDuration && !cancellationToken.IsCancellationRequested)
                        {
                            await Awaitable.NextFrameAsync();
                        }
                    }

                    report.LoadSeconds += Now - phaseStart;
                    SetState(SceneLoadState.Activating);
                    double activateStart = Now;
                    loaded.Add(await ActivateOperationAsync(operation));
                    report.ActivateSeconds += Now - activateStart;
                }

                cancellationToken.ThrowIfCancellationRequested();
                Scene active = PickActive(loaded, activeName);
                if (active.IsValid() && (!additive || options.SetActive) && SceneManager.GetActiveScene() != active)
                {
                    SceneManager.SetActiveScene(active);
                }

                if (hasSetup)
                {
                    SetState(SceneLoadState.SettingUp);
                    double setupStart = Now;
                    await RunSetupAsync(active, loaded, options, new SubProgress(overall, sceneShare, 1f), status, cancellationToken);
                    report.SetupSeconds = Now - setupStart;
                }

                overall.Report(1f);
                if (options.UnloadUnusedAssets)
                {
                    AsyncOperation unload = Resources.UnloadUnusedAssets();
                    while (!unload.isDone)
                    {
                        await Awaitable.NextFrameAsync();
                    }
                }

                for (int i = 0; i < options.SettleFrames; i++)
                {
                    await Awaitable.NextFrameAsync();
                }

                report.Succeeded = true;
                foreach (Scene scene in loaded)
                {
                    Invoke(SceneLoaded, scene);
                }

                return loaded;
            }
            catch (OperationCanceledException)
            {
                report.Cancelled = true;
                await CleanUpFailedAsync(additive, loaded, preloaded);
                throw;
            }
            catch (Exception ex) when (!string.IsNullOrEmpty(options.FallbackScene))
            {
                report.Error = ex;
                await CleanUpFailedAsync(additive, loaded, preloaded);
                report.RecoveredToFallback = await TryLoadFallbackAsync(options.FallbackScene);
                string recovery = report.RecoveredToFallback ? $"; loaded '{options.FallbackScene}' instead" : "; the fallback scene failed too";
                throw new SceneLoadException($"Loading {string.Join(", ", names)} failed{recovery}.", options.FallbackScene, ex);
            }
            catch (Exception ex)
            {
                report.Error = ex;
                await CleanUpFailedAsync(additive, loaded, preloaded);
                throw;
            }
            finally
            {
                if (shown)
                {
                    SetState(SceneLoadState.HidingTransition);
                    double hideStart = Now;
                    await HideAsync(transition);
                    report.HideSeconds = Now - hideStart;
                }

                if (options.LoadingPriority.HasValue || preloaded != null)
                {
                    Application.backgroundLoadingPriority = restorePriority;
                }

                if (preloaded != null)
                {
                    preloaded.IsConsumed = true;
                    _preloaded = null;
                }

                report.TotalSeconds = Now - started;
                LastReport = report;
                SetState(SceneLoadState.Idle);
                // Finish before the gate passes to a queued request, so LoadFinished always follows its own LoadStarted.
                Invoke(LoadFinished, report);
                Release();
            }
        }

        private static async Awaitable RunSetupAsync(Scene active, IReadOnlyList<Scene> scenes, SceneLoadOptions options, IProgress<float> progress,
            ISceneStatusDisplay status, CancellationToken cancellationToken)
        {
            await options.SetupAsync(new SceneSetupContext(active, scenes, progress, status, cancellationToken));
            cancellationToken.ThrowIfCancellationRequested();
            progress.Report(1f);
        }

        // A cancelled or failed additive load unloads what it opened, so a half prepared level never stays loaded.
        // A single-mode load already replaced the previous scenes, so its scenes stay. A preload that failed before its
        // activation is activated first, because a held load blocks every later load.
        private async Awaitable CleanUpFailedAsync(bool additive, List<Scene> scenes, PreloadedScene preloaded)
        {
            if (preloaded != null && !preloaded.Operation.isDone)
            {
                scenes.Add(await ActivateOperationAsync(preloaded.Operation));
            }

            if (additive)
            {
                await UnloadAllAsync(scenes);
            }
        }

        private static async Awaitable<Scene> ActivateOperationAsync(AsyncOperation operation)
        {
            Scene loaded = default;
            void OnLoaded(Scene scene, LoadSceneMode mode) => loaded = scene;

            SceneManager.sceneLoaded += OnLoaded;
            try
            {
                operation.allowSceneActivation = true;
                while (!operation.isDone)
                {
                    await Awaitable.NextFrameAsync();
                }
            }
            finally
            {
                SceneManager.sceneLoaded -= OnLoaded;
            }

            return loaded;
        }

        private async Awaitable UnloadAllAsync(IReadOnlyList<Scene> scenes)
        {
            for (int i = scenes.Count - 1; i >= 0; i--)
            {
                if (scenes[i].IsValid() && scenes[i].isLoaded)
                {
                    try
                    {
                        await UnloadAsync(scenes[i]);
                    }
                    catch (InvalidOperationException ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }
        }

        private static async Awaitable<bool> TryLoadFallbackAsync(string sceneName)
        {
            try
            {
                AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                if (operation == null)
                {
                    throw MissingScene(sceneName);
                }

                while (!operation.isDone)
                {
                    await Awaitable.NextFrameAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        private static Scene PickActive(IReadOnlyList<Scene> scenes, string activeName)
        {
            foreach (Scene scene in scenes)
            {
                if (activeName != null && string.Equals(scene.name, activeName, StringComparison.Ordinal))
                {
                    return scene;
                }
            }

            return scenes.Count > 0 ? scenes[0] : default;
        }

        private static async Awaitable HideAsync(ISceneTransition transition)
        {
            try
            {
                await transition.HideAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        #endregion

        #region GATE

        private async Awaitable<bool> AcquireAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_gateHeld)
            {
                _gateHeld = true;
                return true;
            }

            switch (RequestPolicy)
            {
                case SceneRequestPolicy.Ignore:
                    return false;
                case SceneRequestPolicy.Throw:
                    throw new InvalidOperationException("A scene load is already running.");
            }

            // Release hands the gate straight to the next waiter, so it is still held when this resumes.
            AwaitableCompletionSource turn = new();
            _waiting.Enqueue(turn);
            using CancellationTokenRegistration registration = cancellationToken.Register(() => turn.TrySetCanceled());
            await turn.Awaitable;
            return true;
        }

        private void Release()
        {
            while (_waiting.Count > 0)
            {
                if (_waiting.Dequeue().TrySetResult())
                {
                    return;
                }
            }

            _gateHeld = false;
        }

        private void ThrowIfNotHeld(PreloadedScene handle)
        {
            Guard.NotNull(handle, nameof(handle));
            if (handle.IsConsumed || handle != _preloaded)
            {
                throw new InvalidOperationException($"The preload of '{handle.SceneName}' was already activated or cancelled.");
            }

            if (State != SceneLoadState.Preloaded)
            {
                throw new InvalidOperationException($"'{handle.SceneName}' is still preloading. Await PreloadAsync first.");
            }
        }

        #endregion

        #region STATE

        private static double Now => Time.realtimeSinceStartupAsDouble;

        private static ArgumentException MissingScene(string sceneName)
        {
            return new ArgumentException($"Scene '{sceneName}' could not be loaded. Add it to the scene list of the build profile.");
        }

        private void SetState(SceneLoadState state)
        {
            if (State == state)
            {
                return;
            }

            State = state;
            Invoke(StateChanged, state);
        }

        private void ResetProgress()
        {
            Progress = 0f;
            Invoke(ProgressChanged, 0f);
        }

        private void SetProgress(float value)
        {
            Progress = value;
            Invoke(ProgressChanged, value);
        }

        private void ReportBoth(IProgress<float> progress, float value)
        {
            progress?.Report(value);
            if (value > Progress)
            {
                SetProgress(value);
            }
        }

        // A throwing listener must not break a load in the middle.
        private static void Invoke<T>(Action<T> handler, T value)
        {
            if (handler == null)
            {
                return;
            }

            try
            {
                handler(value);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        #endregion

        private sealed class SceneEntry
        {
            public readonly string Name;
            public readonly Func<LoadSceneMode, AsyncOperation> Begin;

            private SceneEntry(string name, Func<LoadSceneMode, AsyncOperation> begin)
            {
                Name = name;
                Begin = begin;
            }

            public static SceneEntry ByName(string name) => new(name, mode => SceneManager.LoadSceneAsync(name, mode));

            public static SceneEntry ByIndex(int buildIndex) => new("#" + buildIndex, mode => SceneManager.LoadSceneAsync(buildIndex, mode));
        }

        // Forwards to the caller, the transition and the loader's Progress, and never moves backwards.
        private sealed class OverallProgress : IProgress<float>
        {
            private readonly IProgress<float> _target;
            private readonly ISceneTransition _transition;
            private readonly Action<float> _onChanged;
            private float _value = -1f;

            public OverallProgress(IProgress<float> target, ISceneTransition transition, Action<float> onChanged)
            {
                _target = target;
                _transition = transition;
                _onChanged = onChanged;
            }

            public void Report(float value)
            {
                value = Mathf.Clamp01(value);
                if (value <= _value)
                {
                    return;
                }

                _value = value;
                _target?.Report(value);
                _transition?.ReportProgress(value);
                _onChanged(value);
            }
        }
    }
}
