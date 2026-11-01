using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Pretzel.Events;
using Pretzel.Integrations.VContainer;
using Pretzel.Multiplayer;
using Pretzel.Scenes;
using SpaceInvaders.Coop;
using SpaceInvaders.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace SpaceInvaders.Tests
{
    /// <summary>
    /// Plays the real Coop scene on a real NetworkManager host (loopback, two local players) through the real composition root: ships spawn
    /// for each player, the host runs the waves, scores and lives are per player, and the match ends and restarts. Only the host side runs
    /// here; a second machine is covered by the replication tests and by playing online.
    /// </summary>
    public sealed class CoopFlowTests
    {
        private PretzelNetworkBootstrap _bootstrap;
        private CoopMatch _match;
        private InvaderFormation _formation;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            SceneLoader loader = Root().Container.Resolve<SceneLoader>();
            yield return Until(() => !loader.IsBusy, "the scene loader to finish the previous test's load");
            yield return SceneManager.LoadSceneAsync(GameNavigator.TitleScene);
            Root().Container.Resolve<IGameService>().ReturnToMenu();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            if (_bootstrap != null && _bootstrap.Service != null && _bootstrap.Service.State != ConnectionState.Disconnected)
            {
                Task leave = _bootstrap.Service.LeaveAsync();
                yield return Wait(leave);
            }

            _bootstrap = null;
            _match = null;
            _formation = null;
            yield return SceneManager.LoadSceneAsync(GameNavigator.TitleScene);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator A_local_match_spawns_a_ship_for_each_player_and_starts_the_first_wave()
        {
            yield return StartLocalMatch(2, "8-coop-lobby");
            yield return new WaitForSeconds(1.5f);
            yield return Shot("7-coop-local");

            Assert.AreEqual(2, _match.Ships.Count);
            Assert.AreNotEqual(_match.Ships[0].Slot, _match.Ships[1].Slot);
            Assert.AreEqual(CoopPhase.Playing, _match.Phase);
            Assert.AreEqual(1, _match.Wave);
            Assert.IsTrue(_formation.IsRunning, "The host's formation marches.");
            Assert.AreEqual(2, _match.StatCount);
            foreach (CoopPlayerStat stat in Stats())
            {
                Assert.AreEqual(CoopRunState.StartingLives, stat.Lives);
                Assert.AreEqual(0, stat.Score);
            }
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator A_bullet_scores_for_the_player_who_fired_it_and_is_recycled()
        {
            yield return StartLocalMatch(2);
            CoopShip shooter = ShipInSlot(1);
            Invader target = LowestInColumn(_formation, 5);
            shooter.transform.position = new Vector3(target.transform.position.x, shooter.transform.position.y, 0f);
            shooter.Input = new ScriptedInput { Fire = true };
            int startingAlive = _formation.AliveCount;

            yield return Until(() => _formation.AliveCount < startingAlive, "the bullet to kill an invader");
            shooter.Input = new ScriptedInput();
            yield return null;

            Assert.Greater(StatFor(1).Score, 0, "The shooter scores.");
            Assert.AreEqual(0, StatFor(0).Score, "The other player does not.");
            PooledNetworkPrefabHandler bullets = _bootstrap.PooledHandlers[0];
            yield return Until(() => bullets.CountActive == 0, "the bullet to return to its pool");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator A_bomb_costs_only_the_ship_it_hits_and_the_ship_is_briefly_safe()
        {
            yield return StartLocalMatch(2);
            CoopShip hit = ShipInSlot(0);

            Assert.IsTrue(hit.TakeHit(Team.Invaders));
            Assert.IsFalse(hit.TakeHit(Team.Invaders), "The ship is invulnerable right after a hit.");
            Assert.IsFalse(hit.TakeHit(Team.Player), "Only invaders hurt a ship.");

            Assert.AreEqual(CoopRunState.StartingLives - 1, StatFor(0).Lives);
            Assert.AreEqual(CoopRunState.StartingLives, StatFor(1).Lives);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator One_player_running_out_of_lives_does_not_end_the_match_but_both_do()
        {
            yield return StartLocalMatch(2);
            CoopShip first = ShipInSlot(0);
            for (int life = 0; life < CoopRunState.StartingLives; life++)
            {
                Assert.IsTrue(first.TakeHit(Team.Invaders), "Hit number " + (life + 1));
                yield return new WaitForSeconds(2.2f);
            }

            Assert.IsFalse(first.IsAlive, "The first ship is out.");
            Assert.AreEqual(0, StatFor(0).Lives);
            Assert.AreEqual(CoopPhase.Playing, _match.Phase, "The second player is still in the game.");

            Root().Container.Resolve<IEventBus>().Publish(new InvadersLanded());

            Assert.AreEqual(CoopPhase.GameOver, _match.Phase);
            Assert.AreEqual(0, StatFor(1).Lives, "Invaders landing takes every life.");
            Assert.IsFalse(_formation.IsRunning);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator A_player_who_leaves_for_good_does_not_stop_the_match_from_ending()
        {
            yield return StartLocalMatch(2);
            CoopShip leaver = ShipInSlot(1);

            // What the reconnect grace window does when it runs out: the player has no controller, then their ship is despawned.
            leaver.GetComponent<NetworkPlayerIdentity>().ServerSetController(NetworkPlayerIdentity.NoController);
            leaver.NetworkObject.Despawn(true);
            yield return Until(() => _match.StatCount == 1, "the leaver's entry to be removed");
            Assert.AreEqual(CoopPhase.Playing, _match.Phase, "One player is still in the game.");

            CoopShip stayer = ShipInSlot(0);
            for (int life = 0; life < CoopRunState.StartingLives; life++)
            {
                Assert.IsTrue(stayer.TakeHit(Team.Invaders), "Hit number " + (life + 1));
                yield return new WaitForSeconds(2.2f);
            }

            Assert.AreEqual(CoopPhase.GameOver, _match.Phase, "The last player out ends the match; the departed one does not hold it open.");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator Back_to_the_lobby_clears_the_world_and_a_new_match_starts_over()
        {
            yield return StartLocalMatch(2);
            ShipInSlot(0).TakeHit(Team.Invaders);
            Root().Container.Resolve<IEventBus>().Publish(new InvadersLanded());
            Assert.AreEqual(CoopPhase.GameOver, _match.Phase);
            yield return new WaitForSeconds(0.5f);
            yield return Shot("9-coop-game-over");

            Task<bool> end = _bootstrap.Service.EndMatchAsync();
            yield return Wait(end);
            Assert.IsTrue(end.Result);
            yield return Until(() => _match.Phase == CoopPhase.Lobby, "the match to return to the lobby");
            Assert.AreEqual(0, _formation.AliveCount, "The invaders are gone.");
            Assert.AreEqual(0, _match.Wave);

            Task<bool> again = _bootstrap.Service.StartMatchAsync();
            yield return Wait(again);
            Assert.IsTrue(again.Result);
            yield return Until(() => _match.Phase == CoopPhase.Playing && _formation.AliveCount > 0 && _match.Wave == 1, "the new match to start wave one");

            Assert.IsTrue(ShipInSlot(0).IsAlive);
            foreach (CoopPlayerStat stat in Stats())
            {
                Assert.AreEqual(CoopRunState.StartingLives, stat.Lives);
                Assert.AreEqual(0, stat.Score);
            }
        }

        private IEnumerator StartLocalMatch(int localPlayers, string lobbyShot = null)
        {
            _ = Root().Container.Resolve<IGameNavigator>().PlayCoopAsync();
            yield return Until(
                () => IsActive(GameNavigator.CoopScene) && Object.FindAnyObjectByType<PretzelNetworkBootstrap>() != null && Object.FindAnyObjectByType<CoopMatch>() != null,
                "the Coop scene to load");
            _bootstrap = Object.FindAnyObjectByType<PretzelNetworkBootstrap>();
            _match = Object.FindAnyObjectByType<CoopMatch>();
            _formation = Object.FindAnyObjectByType<InvaderFormation>();

            Task<bool> start = _bootstrap.Service.StartLocalAsync(new HostRequest { DisplayName = "Ada", LocalPlayerCount = localPlayers });
            yield return Wait(start);
            Assert.IsTrue(start.Result, "The local host starts.");
            if (lobbyShot != null)
            {
                yield return new WaitForSeconds(0.8f);
                yield return Shot(lobbyShot);
            }

            Task<bool> match = _bootstrap.Service.StartMatchAsync();
            yield return Wait(match);
            Assert.IsTrue(match.Result, "The host starts the match.");
            yield return Until(
                () => _match.Ships.Count == localPlayers && _match.Phase == CoopPhase.Playing && _formation.AliveCount > 0,
                "the ships to spawn and the first wave to start",
                20f);
        }

        private static string ShotFolder => Environment.GetEnvironmentVariable("SI_SCREENSHOT_DIR") ?? Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Temp", "TestScreenshots");

        private static IEnumerator Shot(string name)
        {
            yield return ScreenshotUtility.Capture(Path.Combine(ShotFolder, name + ".png"));
        }

        private List<CoopPlayerStat> Stats()
        {
            List<CoopPlayerStat> stats = new();
            _match.CopyStats(stats);
            return stats;
        }

        private CoopPlayerStat StatFor(int slot)
        {
            foreach (CoopPlayerStat stat in Stats())
            {
                if (stat.Slot == slot)
                {
                    return stat;
                }
            }

            Assert.Fail("No stats for slot " + slot);
            return default;
        }

        private CoopShip ShipInSlot(int slot)
        {
            CoopShip ship = _match.FindShip(slot);
            Assert.IsNotNull(ship, "A ship exists for slot " + slot);
            return ship;
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

            Assert.IsNotNull(lowest, "The formation has an invader in column " + column);
            return lowest;
        }

        private static bool IsActive(string sceneName)
        {
            return string.Equals(SceneManager.GetActiveScene().name, sceneName, StringComparison.Ordinal);
        }

        private static LifetimeScope Root()
        {
            return VContainerSettings.Instance.GetOrCreateRootLifetimeScopeInstance();
        }

        private static IEnumerator Wait(Task task)
        {
            float deadline = Time.realtimeSinceStartup + 20f;
            while (!task.IsCompleted)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail("A session operation did not finish in 20 s.");
                }

                yield return null;
            }
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

        private sealed class ScriptedInput : ICoopShipInput
        {
            public float Horizontal { get; set; }

            public bool Fire { get; set; }

            public float ReadHorizontal(int localPlayerIndex, bool acceptEveryScheme)
            {
                return Horizontal;
            }

            public bool ReadFire(int localPlayerIndex, bool acceptEveryScheme)
            {
                return Fire;
            }
        }
    }
}
