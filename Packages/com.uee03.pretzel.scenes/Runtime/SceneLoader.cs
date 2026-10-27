using System;
using System.Threading;
using Pretzel.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pretzel.Scenes
{
    /// <summary>
    /// Loads and unloads scenes one at a time. Loading is gated at 90 %, so the transition, the minimum duration and the progress
    /// report finish before the scene activates. Unity cannot abort a scene load, so cancellation after loading started
    /// unloads an additive scene again, and lets a single-mode load finish.
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
            bool shown = false;
            IsBusy = true;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (transition != null)
                {
                    shown = true;
                    await transition.ShowAsync(cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();
                Scene loaded = await LoadGatedAsync(begin, label, options, transition, cancellationToken);
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

                if (options.UnloadUnusedAssets)
                {
                    AsyncOperation unload = Resources.UnloadUnusedAssets();
                    while (!unload.isDone)
                    {
                        await Awaitable.NextFrameAsync();
                    }
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
            ISceneTransition transition, CancellationToken cancellationToken)
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
                    Report(options, transition, operation.progress / 0.9f);
                    await Awaitable.NextFrameAsync();
                }

                Report(options, transition, 1f);
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

        private static void Report(SceneLoadOptions options, ISceneTransition transition, float progress)
        {
            options.Progress?.Report(progress);
            transition?.ReportProgress(progress);
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
    }
}
