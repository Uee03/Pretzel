using System;
using Pretzel.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pretzel.Samples.LevelLoading
{
    /// <summary>
    /// Lives in a bootstrap scene that stays loaded. Loads each level additively behind the loading screen, lets the level's
    /// <see cref="LevelSetup"/> spawn its content, and unloads the previous level while the screen still covers it.
    /// </summary>
    public sealed class LevelLoader : MonoBehaviour
    {
        [SerializeField] private UIDocumentSceneTransition _loadingScreen;
        [SerializeField] private string[] _levels = { "LevelA", "LevelB" };
        [SerializeField, Min(0f)] private float _minimumSeconds = 0.5f;

        [Tooltip("Share of the progress bar for loading the scene file. Spawning fills the rest.")]
        [SerializeField, Range(0.1f, 0.9f)] private float _sceneShare = 0.4f;

        [SerializeField] private bool _loadFirstLevelOnStart = true;

        private readonly SceneLoader _loader = new();
        private Scene _current;
        private int _index = -1;

        private void Start()
        {
            if (_loadFirstLevelOnStart)
            {
                LoadNext();
            }
        }

        [ContextMenu("Load Next Level")]
        public void LoadNext() => _ = LoadNextAsync();

        private async Awaitable LoadNextAsync()
        {
            if (_loader.IsBusy || _levels.Length == 0)
            {
                return;
            }

            _index = (_index + 1) % _levels.Length;
            string level = _levels[_index];
            Scene previous = _current;
            SceneLoadOptions options = new()
            {
                Mode = LoadSceneMode.Additive,
                SetActive = true,
                Transition = _loadingScreen,
                MinimumDuration = _minimumSeconds,
                SceneProgressWeight = _sceneShare,
                LoadingStatus = $"Loading {level}",
                SetupAsync = context => SetupLevelAsync(context, previous)
            };

            try
            {
                _current = await _loader.LoadAsync(level, options, destroyCancellationToken);
                Debug.Log($"{level} is ready.", this);
            }
            catch (OperationCanceledException)
            {
                // The loader was destroyed, for example when leaving Play mode.
            }
            catch (ArgumentException ex)
            {
                Debug.LogError(ex.Message, this);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
            }
        }

        private async Awaitable SetupLevelAsync(SceneSetupContext context, Scene previous)
        {
            if (previous.IsValid() && previous.isLoaded)
            {
                context.ReportStatus("Unloading the previous level");
                await _loader.UnloadAsync(previous, context.CancellationToken);
            }

            LevelSetup setup = context.FindInScene<LevelSetup>();
            if (setup == null)
            {
                Debug.LogWarning($"{context.Scene.name} has no LevelSetup, so nothing was spawned.", this);
                return;
            }

            await setup.PrepareAsync(context);
        }
    }
}
