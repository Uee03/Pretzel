using System;
using System.Threading;
using Pretzel.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pretzel.Scenes
{
    /// <summary>
    /// Loads and unloads scenes one at a time. Loading is gated at 90 %, so the transition, the minimum duration and the progress
    /// report finish before the scene activates. After activation, <see cref="SceneLoadOptions.SetupAsync"/> runs behind the
    /// transition and fills the rest of the progress bar. Unity cannot abort a scene load, so cancellation after loading
    /// started unloads an additive scene again, and lets a single-mode load finish.
    /// </summary>
    public sealed class SceneLoader
    {
        public bool IsBusy { get; private set; }

        public event Action<Scene> SceneLoaded;

        public Awaitable<Scene> LoadAsync(string sceneName, SceneLoadOptions options = null, CancellationToken cancellationToken = default)
        {
            Guard.NotNullOrEmpty(sceneName, nameof(sceneName));
            return LoadCoreAsync(mode => SceneManager.LoadSceneAsync(sceneName, mode), sceneName, options, cancellationToken);
        }

        public Awaitable<Scene> LoadAsync(int buildIndex, SceneLoadOptions options = null, CancellationToken cancellationToken = default)
        {
            Guard.InRange(buildIndex, 0, int.MaxValue, nameof(buildIndex));
            return LoadCoreAsync(mode => SceneManager.LoadSceneAsync(buildIndex, mode), "#" + buildIndex, options, cancellationToken);
        }

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

        private async Awaitable<Scene> LoadCoreAsync(Func<LoadSceneMode, AsyncOperation> begin, string label, SceneLoadOptions options, CancellationToken cancellationToken)
        {
            if (IsBusy)
            {
                throw new InvalidOperationException("A scene load is already running.");
            }

            options ??= new SceneLoadOptions();
            ISceneTransition transition = options.Transition;
            ISceneStatusDisplay status = transition as ISceneStatusDisplay;
            OverallProgress overall = new(options.Progress, transition);
            bool hasSetup = options.SetupAsync != null;
            float sceneShare = hasSetup ? Mathf.Clamp01(options.SceneProgressWeight) : 1f;
            bool shown = false;
            IsBusy = true;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (transition != null)
                {
                    shown = true;
                    if (options.LoadingStatus != null)
                    {
                        status?.ReportStatus(options.LoadingStatus);
                    }

                    await transition.ShowAsync(cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();
                Scene loaded = await LoadGatedAsync(begin, label, options, new SubProgress(overall, 0f, sceneShare), cancellationToken);
                bool additive = options.Mode == LoadSceneMode.Additive;
                if (cancellationToken.IsCancellationRequested && additive)
                {
                    await UnloadAsync(loaded, CancellationToken.None);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                if (additive && options.SetActive)
                {
                    SceneManager.SetActiveScene(loaded);
                }

                if (hasSetup)
                {
                    await RunSetupAsync(loaded, options, new SubProgress(overall, sceneShare, 1f), status, additive, cancellationToken);
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

                SceneLoaded?.Invoke(loaded);
                return loaded;
            }
            finally
            {
                if (shown)
                {
                    await HideAsync(transition);
                }

                IsBusy = false;
            }
        }

        private static async Awaitable<Scene> LoadGatedAsync(Func<LoadSceneMode, AsyncOperation> begin, string label, SceneLoadOptions options,
            IProgress<float> progress, CancellationToken cancellationToken)
        {
            Scene loaded = default;
            void OnLoaded(Scene scene, LoadSceneMode mode) => loaded = scene;

            float start = Time.realtimeSinceStartup;
            SceneManager.sceneLoaded += OnLoaded;
            try
            {
                AsyncOperation operation = begin(options.Mode);
                if (operation == null)
                {
                    throw new ArgumentException($"Scene '{label}' could not be loaded. Add it to the scene list of the build profile.");
                }

                operation.allowSceneActivation = false;
                while (operation.progress < 0.9f)
                {
                    progress.Report(operation.progress / 0.9f);
                    await Awaitable.NextFrameAsync();
                }

                progress.Report(1f);
                while (Time.realtimeSinceStartup - start < options.MinimumDuration && !cancellationToken.IsCancellationRequested)
                {
                    await Awaitable.NextFrameAsync();
                }

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

        // Cancelling or failing during setup unloads an additive scene, so it is never left half prepared.
        private async Awaitable RunSetupAsync(Scene scene, SceneLoadOptions options, IProgress<float> progress, ISceneStatusDisplay status,
            bool additive, CancellationToken cancellationToken)
        {
            try
            {
                await options.SetupAsync(new SceneSetupContext(scene, progress, status, cancellationToken));
                cancellationToken.ThrowIfCancellationRequested();
                progress.Report(1f);
            }
            catch when (additive && scene.isLoaded)
            {
                await UnloadAsync(scene, CancellationToken.None);
                throw;
            }
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

        // Forwards to the caller and the transition, and never moves backwards.
        private sealed class OverallProgress : IProgress<float>
        {
            private readonly IProgress<float> _target;
            private readonly ISceneTransition _transition;
            private float _value = -1f;

            public OverallProgress(IProgress<float> target, ISceneTransition transition)
            {
                _target = target;
                _transition = transition;
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
            }
        }
    }
}
