using System;
using System.Collections;
using NUnit.Framework;
using Pretzel.Scenes;
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
    /// What a co-op client does with the host's snapshots, run on the single-player scene's real actors: a mirrored formation that never marches
    /// follows the host's block, frame and survivors; the mystery ship and the bunkers show what the host reports.
    /// </summary>
    public sealed class CoopMirrorTests
    {
        private GameObject _clone;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            SceneLoader loader = Root().Container.Resolve<SceneLoader>();
            yield return Until(() => !loader.IsBusy, "the scene loader to finish the previous test's load");
            yield return SceneManager.LoadSceneAsync(GameNavigator.TitleScene);
            Root().Container.Resolve<IGameService>().ReturnToMenu();
            _ = Root().Container.Resolve<IGameNavigator>().PlayAsync();
            yield return Until(() => IsGameActive() && Object.FindAnyObjectByType<InvaderFormation>()?.AliveCount == 55, "the first wave to spawn");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_clone != null)
            {
                Object.Destroy(_clone);
            }

            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator A_mirrored_formation_follows_the_hosts_state_without_marching()
        {
            InvaderFormation host = Object.FindAnyObjectByType<InvaderFormation>();
            _clone = Object.Instantiate(host.gameObject);
            InvaderFormation mirror = _clone.GetComponent<InvaderFormation>();
            mirror.SimulationEnabled = false;
            mirror.Spawn(host.Wave, 0);

            Assert.AreEqual(55, mirror.AliveCount, "The mirror builds the same grid.");
            Assert.IsFalse(mirror.IsRunning, "A mirror never marches by itself.");

            Invader victim = LowestInColumn(host, 5);
            int victimSlot = victim.Row * host.Wave.Columns + victim.Column;
            Assert.IsTrue(victim.TakeHit(Team.Player));
            yield return null;
            host.WriteAliveMask(out ulong low, out ulong high);
            Assert.AreEqual(54, PopCount(low) + PopCount(high));
            Assert.AreEqual(0UL, victimSlot < 64 ? low & (1UL << victimSlot) : high & (1UL << (victimSlot - 64)), "The dead invader's bit is clear.");

            mirror.ApplyMirror(host.transform.position, host.StepIndex, low, high);

            Assert.AreEqual(54, mirror.AliveCount, "The mirror removed the same invader.");
            Assert.AreEqual(host.transform.position.x, mirror.transform.position.x, 0.001f);
            Assert.AreEqual(host.transform.position.y, mirror.transform.position.y, 0.001f);
            foreach (Invader mirrored in mirror.Alive)
            {
                Invader original = Find(host, mirrored.Row, mirrored.Column);
                Assert.IsNotNull(original, $"The host has an invader at row {mirrored.Row}, column {mirrored.Column}.");
                Assert.AreEqual(original.transform.position.x, mirrored.transform.position.x, 0.001f);
                Assert.AreEqual(original.transform.position.y, mirrored.transform.position.y, 0.001f);
            }
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator A_cleared_mirror_has_no_invaders()
        {
            InvaderFormation host = Object.FindAnyObjectByType<InvaderFormation>();
            _clone = Object.Instantiate(host.gameObject);
            InvaderFormation mirror = _clone.GetComponent<InvaderFormation>();
            mirror.SimulationEnabled = false;
            mirror.Spawn(host.Wave, 0);

            mirror.Clear();
            yield return null;

            Assert.AreEqual(0, mirror.AliveCount);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator A_mirrored_mystery_ship_shows_where_the_host_says_without_flying()
        {
            Ufo ufo = Object.FindAnyObjectByType<Ufo>(FindObjectsInactive.Include);

            ufo.Mirror(true, new Vector2(2f, 3.45f));
            yield return null;
            Assert.IsTrue(ufo.gameObject.activeSelf);
            Assert.AreEqual(2f, ufo.transform.position.x, 0.001f);
            Assert.IsFalse(ufo.IsFlying, "A mirrored ship does not fly by itself.");

            ufo.Mirror(false, Vector2.zero);
            Assert.IsFalse(ufo.gameObject.activeSelf);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator Bunker_wear_round_trips_through_the_replicated_health_values()
        {
            BunkerField bunkers = Object.FindAnyObjectByType<BunkerField>();
            byte[] health = new byte[bunkers.BlockCount];
            bunkers.ReadHealths(health);
            Assert.Greater(health.Length, 0);
            Assert.AreEqual(3, health[0], "Bricks start at full health.");

            bunkers.SetHealth(0, 1);
            bunkers.SetHealth(1, 0);
            yield return null;
            bunkers.ReadHealths(health);
            Assert.AreEqual(1, health[0]);
            Assert.AreEqual(0, health[1], "A destroyed brick reads as zero.");

            bunkers.RestoreAll();
            bunkers.ReadHealths(health);
            Assert.AreEqual(3, health[0]);
            Assert.AreEqual(3, health[1]);
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

        private static Invader Find(InvaderFormation formation, int row, int column)
        {
            foreach (Invader invader in formation.Alive)
            {
                if (invader.Row == row && invader.Column == column)
                {
                    return invader;
                }
            }

            return null;
        }

        private static int PopCount(ulong value)
        {
            int count = 0;
            while (value != 0UL)
            {
                count += (int)(value & 1UL);
                value >>= 1;
            }

            return count;
        }

        private static bool IsGameActive()
        {
            return string.Equals(SceneManager.GetActiveScene().name, GameNavigator.GameScene, StringComparison.Ordinal);
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
