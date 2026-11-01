using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using Pretzel.Data;
using Pretzel.DevConsole;
using Pretzel.Integrations.VContainer;
using Pretzel.Scenes;
using SpaceInvaders.Gameplay;
using SpaceInvaders.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace SpaceInvaders.Tests
{
    /// <summary>
    /// Plays the real scenes through the real composition root: boot, play a wave, shoot an invader with physics, clear the wave,
    /// lose all lives, and go back to the title. Screenshots land in Temp/TestScreenshots.
    /// </summary>
    public sealed class GameFlowTests
    {
        private static readonly DataKey _progressKey = new(GameService.ProgressName, null, GameService.SaveFolder);

        private IDataStore _store;
        private bool _hadProgress;

        private static string ShotFolder => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Temp", "TestScreenshots");

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            SceneLoader loader = Root().Container.Resolve<SceneLoader>();
            yield return Until(() => !loader.IsBusy, "the scene loader to finish the previous test's load");
            Step("setup: load title");
            yield return SceneManager.LoadSceneAsync(GameNavigator.TitleScene);
            Root().Container.Resolve<IGameService>().ReturnToMenu();
            _store = Root().Container.Resolve<IDataStore>();
            _hadProgress = _store.Exists(_progressKey);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            if (!_hadProgress && _store != null)
            {
                _store.Delete(_progressKey);
            }

            yield return null;
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator Title_boots_the_root_scope_and_loads_saved_progress()
        {
            IGameService game = Root().Container.Resolve<IGameService>();

            yield return Until(() => game.IsLoaded, "saved progress to load");

            Assert.AreEqual(GameState.Menu, game.State);
            TitleView title = Object.FindAnyObjectByType<TitleView>();
            Assert.IsNotNull(title, "The title scene has a TitleView.");
            yield return Until(() => title.Root != null && title.Root.Q<Button>("play") != null, "the title UI to build");
            yield return Shot("1-title");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator A_full_game_loop_works_from_play_to_game_over_and_back()
        {
            LifetimeScope root = Root();
            IGameService game = root.Container.Resolve<IGameService>();
            IGameNavigator navigator = root.Container.Resolve<IGameNavigator>();
            yield return Until(() => game.IsLoaded, "saved progress to load");

            Step("play");
            _ = navigator.PlayAsync();
            yield return Until(() => IsActive(GameNavigator.GameScene) && FormationOrNull()?.AliveCount == 55, "the first wave to spawn");
            Step("wave 1 spawned");

            InvaderFormation formation = FormationOrNull();
            PlayerShip player = Object.FindAnyObjectByType<PlayerShip>();
            RunState run = LevelScope().Container.Resolve<RunState>();
            Assert.AreEqual(GameState.Playing, game.State);
            Assert.AreEqual(1, run.Wave);
            Assert.AreEqual(RunState.StartingLives, run.Lives);
            yield return new WaitForSeconds(0.6f);
            yield return Shot("2-wave-1");

            // A real bullet, through the pool and the physics triggers.
            Invader target = LowestInColumn(formation, 5);
            player.transform.position = new Vector3(target.transform.position.x, player.transform.position.y, 0f);
            Assert.IsTrue(player.TryFire(), "The ship fires when its cooldown is ready.");
            Assert.IsFalse(player.TryFire(), "The cooldown blocks a second shot at once.");
            yield return Until(() => formation.AliveCount < 55, "the bullet to kill an invader");
            yield return null;
            yield return Shot("3-invader-destroyed");
            Assert.Greater(game.Score, 0, "Killing an invader scores.");

            ProjectileSpawner projectiles = Object.FindAnyObjectByType<ProjectileSpawner>();
            yield return Until(() => projectiles.CountActive(ProjectileKind.Bomb) > 0, "the invaders to drop a bomb");

            formation.KillAll();
            yield return Until(() => run.Wave == 2 && formation.AliveCount == 55, "wave 2 to start after wave 1 is cleared");

            Assert.IsTrue(player.TakeHit(Team.Invaders));
            Assert.AreEqual(2, run.Lives);
            Assert.IsFalse(player.TakeHit(Team.Invaders), "The ship is invulnerable right after a hit.");
            Assert.AreEqual(2, run.Lives);
            yield return new WaitForSeconds(2.2f);
            Assert.IsTrue(player.TakeHit(Team.Invaders));
            yield return new WaitForSeconds(2.2f);
            int scoreBeforeEnd = game.Score;
            Assert.IsTrue(player.TakeHit(Team.Invaders));

            Assert.AreEqual(GameState.GameOver, game.State);
            Assert.AreEqual(0, run.Lives);
            Assert.AreEqual(scoreBeforeEnd, game.HighScore, "The finished game becomes the high score.");
            HudView hud = Object.FindAnyObjectByType<HudView>();
            yield return new WaitForSeconds(0.4f);
            Assert.AreNotEqual(DisplayStyle.None, hud.Root.Q<VisualElement>("game-over").resolvedStyle.display, "The game-over screen is showing.");
            yield return Shot("4-game-over");

            _ = navigator.ReturnToTitleAsync();
            yield return Until(() => IsActive(GameNavigator.TitleScene), "the title scene to load");
            Assert.AreEqual(GameState.Menu, game.State);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator Dev_console_commands_drive_the_running_game()
        {
            LifetimeScope root = Root();
            IGameService game = root.Container.Resolve<IGameService>();
            _ = root.Container.Resolve<IGameNavigator>().PlayAsync();
            yield return Until(() => IsActive(GameNavigator.GameScene) && FormationOrNull()?.AliveCount == 55, "the first wave to spawn");
            DeveloperConsole console = Object.FindAnyObjectByType<DeveloperConsole>();
            RunState run = LevelScope().Container.Resolve<RunState>();
            PlayerShip player = Object.FindAnyObjectByType<PlayerShip>();

            console.Submit("score 500");
            Assert.AreEqual(500, game.Score);

            console.Submit("wave 3");
            Assert.AreEqual(3, run.Wave);
            Assert.AreEqual(55, FormationOrNull().AliveCount, "Wave 3 is 11 columns of 5 rows.");

            console.Submit("god");
            Assert.IsTrue(player.GodMode);
            Assert.IsFalse(player.TakeHit(Team.Invaders), "God mode ignores hits.");

            console.Submit("lives 7");
            Assert.AreEqual(7, run.Lives);

            console.Submit("clear");
            Assert.AreEqual(0, FormationOrNull().AliveCount);
            Assert.AreEqual(500 + 1100, game.Score, "Every invader of wave 3 paid out: (30 + 3 x 20 + 10) x 11 columns.");

            SceneLoader loader = root.Container.Resolve<SceneLoader>();
            yield return Until(() => !loader.IsBusy, "the loading screen to finish");
            console.Open();
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(console.IsOpen);
            yield return Shot("6-dev-console");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator Pausing_stops_time_and_shows_the_pause_screen()
        {
            LifetimeScope root = Root();
            IGameService game = root.Container.Resolve<IGameService>();
            _ = root.Container.Resolve<IGameNavigator>().PlayAsync();
            yield return Until(() => IsActive(GameNavigator.GameScene) && FormationOrNull()?.AliveCount == 55, "the first wave to spawn");
            HudView hud = Object.FindAnyObjectByType<HudView>();

            game.Pause();
            hud.ShowPause();
            yield return new WaitForSecondsRealtime(0.4f);

            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(hud.IsPauseVisible);
            yield return Shot("5-paused");

            game.Resume();
            hud.HidePause();
            Assert.AreEqual(1f, Time.timeScale);
        }

        private static bool IsActive(string sceneName)
        {
            return string.Equals(SceneManager.GetActiveScene().name, sceneName, StringComparison.Ordinal);
        }

        private static LifetimeScope Root()
        {
            return VContainerSettings.Instance.GetOrCreateRootLifetimeScopeInstance();
        }

        private static LifetimeScope LevelScope()
        {
            foreach (PretzelLifetimeScope scope in Object.FindObjectsByType<PretzelLifetimeScope>())
            {
                if (string.Equals(scope.gameObject.scene.name, GameNavigator.GameScene, StringComparison.Ordinal))
                {
                    return scope;
                }
            }

            throw new InvalidOperationException("The Game scene has no level scope.");
        }

        private static InvaderFormation FormationOrNull()
        {
            return Object.FindAnyObjectByType<InvaderFormation>();
        }

        private static Invader LowestInColumn(InvaderFormation formation, int column)
        {
            Invader lowest = null;
            foreach (Invader invader in formation.Alive)
            {
                if (invader.Column == column && (lowest == null || invader.transform.position.y < lowest.transform.position.y))
                {
                    lowest = invader;
                }
            }

            return lowest;
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

        private static void Step(string message) => Debug.Log("[SI] " + message);

        private static IEnumerator Shot(string name)
        {
            Step("shot " + name);
            yield return ScreenshotUtility.Capture(Path.Combine(ShotFolder, name + ".png"));
        }
    }
}
