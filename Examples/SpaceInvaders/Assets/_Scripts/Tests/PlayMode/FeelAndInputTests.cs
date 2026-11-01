using System;
using System.Collections;
using NUnit.Framework;
using Pretzel.Audio;
using Pretzel.Cameras;
using Pretzel.Events;
using Pretzel.Parallax;
using Pretzel.Integrations.VContainer;
using Pretzel.Scenes;
using Pretzel.Vfx;
using SpaceInvaders.Audio;
using SpaceInvaders.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace SpaceInvaders.Tests
{
    /// <summary>
    /// The Pretzel modules at work inside the real game: game feel, the camera and its playfield fit, the scrolling backdrop and
    /// the input contexts. Everything runs through the real scenes and the real composition root.
    /// </summary>
    public sealed class FeelAndInputTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            SceneLoader loader = Root().Container.Resolve<SceneLoader>();
            yield return Until(() => !loader.IsBusy, "the scene loader to finish the previous test's load");
            yield return SceneManager.LoadSceneAsync(GameNavigator.TitleScene);
            Root().Container.Resolve<IGameService>().ReturnToMenu();
            yield return Until(() => Root().Container.Resolve<IGameService>().IsLoaded, "saved progress to load");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            TimeScaleService.Instance.CancelAll();
            yield return null;
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator Being_hit_gives_a_slow_motion_a_shake_and_a_flash_and_time_comes_back()
        {
            yield return StartGame();
            PlayerShip player = Object.FindAnyObjectByType<PlayerShip>();
            CameraShake shake = Object.FindAnyObjectByType<CameraShake>();
            SpriteFlash flash = player.GetComponent<SpriteFlash>();

            Assert.IsTrue(player.TakeHit(Team.Invaders));

            Assert.Less(Time.timeScale, 0.01f, "A hit stop freezes the game at once.");
            Assert.Greater(shake.Trauma, 0.5f, "A hit on the player shakes the camera hard.");
            Assert.IsTrue(flash.IsFlashing);

            yield return Until(() => Time.timeScale > 0.2f && Time.timeScale < 0.5f, "the slow motion after the hit stop", 3f);
            yield return Until(() => Mathf.Approximately(Time.timeScale, 1f), "the time to return to normal", 4f);
            Assert.IsFalse(TimeScaleService.Instance.Stack.IsActive);
            yield return Until(() => shake.Trauma <= 0f, "the shake to settle", 4f);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator A_hit_during_a_pause_never_un_pauses_the_game()
        {
            yield return StartGame();
            IGameService game = Root().Container.Resolve<IGameService>();
            PlayerShip player = Object.FindAnyObjectByType<PlayerShip>();
            Assert.IsTrue(player.TakeHit(Team.Invaders));

            game.Pause();
            yield return Until(() => !TimeScaleService.Instance.Stack.IsActive, "the hit's effects to end", 5f);

            Assert.AreEqual(0f, Time.timeScale, "The pause must survive the effects that overlapped it.");
            game.Resume();
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator The_playfield_stays_fully_visible_on_any_aspect_ratio()
        {
            yield return StartGame();
            Camera camera = Camera.main;
            try
            {
                camera.aspect = 16f / 9f;
                yield return Frames(3);
                Assert.AreEqual(5f, camera.orthographicSize, 0.01f, "On 16:9 the height decides.");

                camera.aspect = 1f;
                yield return Frames(3);
                Assert.AreEqual(8.9f, camera.orthographicSize, 0.01f, "On a square window the 17.8 unit width decides.");

                camera.aspect = 9f / 16f;
                yield return Frames(3);
                Assert.AreEqual(17.8f / 2f / (9f / 16f), camera.orthographicSize, 0.01f, "On a portrait window the width still fits.");
            }
            finally
            {
                camera.ResetAspect();
            }
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator The_star_field_scrolls_by_itself_and_stops_with_the_game()
        {
            yield return StartGame();
            ParallaxController stars = Object.FindAnyObjectByType<ParallaxController>();
            Assert.AreEqual(3, stars.Layers.Count, "Far, mid and near star layers.");
            ParallaxLayer near = stars.Layers[2];
            float start = near.transform.position.y;

            yield return new WaitForSeconds(0.5f);
            float moved = near.transform.position.y;
            Assert.Less(moved, start, "The star field scrolls down.");
            Assert.Greater(Mathf.Abs(stars.Layers[2].ScrollOffset.y), Mathf.Abs(stars.Layers[0].ScrollOffset.y), "Nearer layers scroll faster.");

            Root().Container.Resolve<IGameService>().Pause();
            yield return new WaitForSecondsRealtime(0.1f);
            float frozen = near.transform.position.y;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(frozen, near.transform.position.y, 0.0001f, "A paused game has a still backdrop.");
            Root().Container.Resolve<IGameService>().Resume();
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator Pausing_is_an_input_context_change_and_the_UI_stays_live()
        {
            yield return StartGame();
            GameInput input = Root().Container.Resolve<GameInput>();
            InputActionMap player = InputSystem.actions.FindActionMap("Player", true);
            InputActionMap ui = InputSystem.actions.FindActionMap("UI", true);

            Assert.AreEqual(GameInput.PlayerContext, input.Router.ActiveContext);
            Assert.IsTrue(player.enabled);
            Assert.IsTrue(ui.enabled);

            input.Pause();
            Assert.IsFalse(player.enabled, "The ship cannot be steered while paused.");
            Assert.IsTrue(ui.enabled, "The pause menu still needs the UI map.");

            input.Resume();
            Assert.IsTrue(player.enabled);
            input.Resume();
            Assert.IsTrue(player.enabled, "Resuming when not paused changes nothing.");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator Input_is_blocked_while_a_scene_loads_and_comes_back()
        {
            yield return StartGame();
            GameInput input = Root().Container.Resolve<GameInput>();
            IGameNavigator navigator = Root().Container.Resolve<IGameNavigator>();

            _ = navigator.ReturnToTitleAsync();
            yield return Until(() => input.Router.Blockers.IsBlocked, "the scene load to block input", 5f);
            Assert.IsNull(input.Router.ActiveContext);

            yield return Until(() => IsActive(GameNavigator.TitleScene) && !input.Router.Blockers.IsBlocked, "the title to load and input to return", 15f);
            Assert.AreEqual(GameInput.PlayerContext, input.Router.ActiveContext);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator Music_follows_the_scene_and_the_wave()
        {
            IAudioService audio = Root().Container.Resolve<IAudioService>();
            yield return Until(() => MusicName(audio) == "title_theme", "the title tune", 5f);

            yield return StartGame();
            yield return Until(() => MusicName(audio) == "gameplay_loop", "the gameplay loop", 5f);

            Root().Container.Resolve<IEventBus>().Publish(new WaveStarted(SoundBank.LastStandFromWave, "Five"));
            yield return Until(() => MusicName(audio) == "last_stand", "the last stand tune", 5f);
        }

        private static string MusicName(IAudioService audio)
        {
            return audio.CurrentMusic == null ? string.Empty : audio.CurrentMusic.name;
        }

        private static IEnumerator StartGame()
        {
            IGameNavigator navigator = Root().Container.Resolve<IGameNavigator>();
            _ = navigator.PlayAsync();
            yield return Until(() => IsActive(GameNavigator.GameScene) && Object.FindAnyObjectByType<InvaderFormation>()?.AliveCount == 55, "the first wave to spawn");
            SceneLoader loader = Root().Container.Resolve<SceneLoader>();
            yield return Until(() => !loader.IsBusy, "the loading screen to finish");
            yield return Frames(2);
        }

        private static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
            }
        }

        private static bool IsActive(string sceneName)
        {
            return string.Equals(SceneManager.GetActiveScene().name, sceneName, StringComparison.Ordinal);
        }

        private static LifetimeScope Root()
        {
            return VContainerSettings.Instance.GetOrCreateRootLifetimeScopeInstance();
        }

        private static IEnumerator Until(Func<bool> condition, string description, float timeoutSeconds = 10f)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail($"Timed out after {timeoutSeconds} s waiting for {description}.");
                }

                yield return null;
            }
        }
    }
}
