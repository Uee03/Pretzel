using System;
using System.Threading;
using Pretzel.Scenes;
using SpaceInvaders.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceInvaders
{
    /// <summary>Loads the title and game scenes behind the loading screen. A second request while one load runs is ignored.</summary>
    public sealed class GameNavigator : IGameNavigator, IDisposable
    {
        public const string TitleScene = "Title";
        public const string GameScene = "Game";

        private readonly SceneLoader _loader;
        private readonly UIDocumentSceneTransition _loadingScreen;
        private readonly IGameService _game;
        private readonly CancellationTokenSource _cancellation = new();

        public GameNavigator(SceneLoader loader, UIDocumentSceneTransition loadingScreen, IGameService game)
        {
            _loader = loader;
            _loadingScreen = loadingScreen;
            _game = game;
            _loader.RequestPolicy = SceneRequestPolicy.Ignore;
        }

        public Awaitable PlayAsync()
        {
            SceneLoadOptions options = new()
            {
                Mode = LoadSceneMode.Single,
                Transition = _loadingScreen,
                MinimumDuration = 0.5f,
                SceneProgressWeight = 0.5f,
                LoadingStatus = "Deploying the invaders",
                SetupAsync = SetUpGameAsync,
                FallbackScene = TitleScene
            };
            return LoadAsync(GameScene, options);
        }

        public Awaitable ReturnToTitleAsync()
        {
            // Leaving a paused game must give the time back before the loading screen fades.
            _game.ReturnToMenu();
            SceneLoadOptions options = new()
            {
                Mode = LoadSceneMode.Single,
                Transition = _loadingScreen,
                MinimumDuration = 0.3f,
                LoadingStatus = "Back to the title"
            };
            return LoadAsync(TitleScene, options);
        }

        public void Dispose()
        {
            _cancellation.Cancel();
            _cancellation.Dispose();
        }

        private static async Awaitable SetUpGameAsync(SceneSetupContext context)
        {
            LevelSetup setup = context.FindInScene<LevelSetup>();
            if (setup != null)
            {
                await setup.PrepareAsync(context);
            }
        }

        private async Awaitable LoadAsync(string sceneName, SceneLoadOptions options)
        {
            try
            {
                await _loader.LoadAsync(sceneName, options, _cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                // The navigator was disposed, for example when leaving Play mode.
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
