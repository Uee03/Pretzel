using System;
using Pretzel.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pretzel.Samples.Scenes
{
    /// <summary>Loads scenes with a loading screen. Call the public methods from buttons or the context menu.</summary>
    public sealed class SceneMenu : MonoBehaviour
    {
        [SerializeField] private UIDocumentSceneTransition _transition;
        [SerializeField] private string _singleScene = "Level1";
        [SerializeField] private string _additiveScene = "Level1Lighting";
        [SerializeField, Min(0f)] private float _minimumSeconds = 1f;

        private readonly SceneLoader _loader = new();

        [ContextMenu("Load Single")]
        public void LoadSingle() => _ = LoadAsync(_singleScene, LoadSceneMode.Single);

        [ContextMenu("Load Additive")]
        public void LoadAdditive() => _ = LoadAsync(_additiveScene, LoadSceneMode.Additive);

        [ContextMenu("Unload Additive")]
        public async void UnloadAdditiveAsync()
        {
            try
            {
                await _loader.UnloadAsync(_additiveScene, destroyCancellationToken);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is OperationCanceledException)
            {
                Debug.LogWarning(ex.Message, this);
            }
        }

        private async Awaitable LoadAsync(string sceneName, LoadSceneMode mode)
        {
            if (_loader.IsBusy)
            {
                return;
            }

            SceneLoadOptions options = new()
            {
                Mode = mode,
                Transition = _transition,
                MinimumDuration = _minimumSeconds,
                UnloadUnusedAssets = mode == LoadSceneMode.Single
            };

            try
            {
                Scene scene = await _loader.LoadAsync(sceneName, options, destroyCancellationToken);
                Debug.Log($"Loaded {scene.name} ({mode}).");
            }
            catch (OperationCanceledException)
            {
                // This menu was destroyed, for example by the single-mode load itself.
            }
            catch (ArgumentException ex)
            {
                Debug.LogError(ex.Message, this);
            }
        }
    }
}
