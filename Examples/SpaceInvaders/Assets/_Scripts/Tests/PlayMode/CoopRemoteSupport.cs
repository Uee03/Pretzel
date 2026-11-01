using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using Pretzel.Integrations.VContainer;
using Pretzel.Multiplayer;
using Pretzel.Scenes;
using SpaceInvaders.Coop;
using SpaceInvaders.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace SpaceInvaders.Tests
{
    /// <summary>
    /// Shared steps of the two-process co-op tests. The processes talk through small marker files in a folder under the user's temp path,
    /// because one Unity project can only be open once: run the host test in one copy of the project and the client test in another.
    /// </summary>
    internal static class CoopRemoteSupport
    {
        public const string HostReady = "host-ready";
        public const string MatchStarted = "match-started";
        public const string ClientDone = "client-done";

        public static string Folder => Path.Combine(Path.GetTempPath(), "SpaceInvadersRemoteCoop");

        /// <summary>Set SI_REMOTE_ONLINE=1 in both processes to go through Relay and the project's Unity Cloud link instead of loopback.</summary>
        public static bool Online => string.Equals(Environment.GetEnvironmentVariable("SI_REMOTE_ONLINE"), "1", StringComparison.Ordinal);

        /// <summary>Two processes on one machine must sign in as two different players, which is what an Authentication Profile is for.</summary>
        public static void UseAuthenticationProfile(PretzelNetworkBootstrap bootstrap, string profile)
        {
            FieldInfo field = typeof(NetworkSettings).GetField("_authenticationProfile", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(bootstrap.Settings, profile);
        }

        public static void Mark(string name, string content = "")
        {
            Directory.CreateDirectory(Folder);
            // Written under another name and then moved, so the other process never finds a half-written marker.
            string temporary = Path.Combine(Folder, name + ".tmp");
            File.WriteAllText(temporary, content);
            File.Move(temporary, Path.Combine(Folder, name));
        }

        public static void ClearMarkers()
        {
            if (Directory.Exists(Folder))
            {
                Directory.Delete(Folder, true);
            }

            Directory.CreateDirectory(Folder);
        }

        public static string Read(string name)
        {
            return File.ReadAllText(Path.Combine(Folder, name));
        }

        public static IEnumerator WaitForMarker(string name, float timeoutSeconds)
        {
            yield return Until(() => File.Exists(Path.Combine(Folder, name)), "the other process to write '" + name + "'", timeoutSeconds);
        }

        public static IEnumerator Until(Func<bool> condition, string description, float timeoutSeconds = 10f)
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

        public static IEnumerator Wait(Task task, float timeoutSeconds = 30f)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!task.IsCompleted)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail($"A session operation did not finish in {timeoutSeconds} s.");
                }

                yield return null;
            }
        }

        public static LifetimeScope Root()
        {
            return VContainerSettings.Instance.GetOrCreateRootLifetimeScopeInstance();
        }

        /// <summary>Boots the root scope on the title and opens the Coop scene through the game's own navigator.</summary>
        public static IEnumerator OpenCoopScene()
        {
            Time.timeScale = 1f;
            SceneLoader loader = Root().Container.Resolve<SceneLoader>();
            yield return Until(() => !loader.IsBusy, "the scene loader to be idle");
            yield return SceneManager.LoadSceneAsync(GameNavigator.TitleScene);
            Root().Container.Resolve<IGameService>().ReturnToMenu();
            _ = Root().Container.Resolve<IGameNavigator>().PlayCoopAsync();
            yield return Until(
                () => string.Equals(SceneManager.GetActiveScene().name, GameNavigator.CoopScene, StringComparison.Ordinal)
                      && Object.FindAnyObjectByType<PretzelNetworkBootstrap>() != null
                      && Object.FindAnyObjectByType<CoopMatch>() != null,
                "the Coop scene to load");
        }

        public static IEnumerator Leave(PretzelNetworkBootstrap bootstrap)
        {
            if (bootstrap != null && bootstrap.Service != null && bootstrap.Service.State != ConnectionState.Disconnected)
            {
                yield return Wait(bootstrap.Service.LeaveAsync());
            }

            yield return SceneManager.LoadSceneAsync(GameNavigator.TitleScene);
        }

        public static Invader LowestInColumn(InvaderFormation formation, int column)
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
    }
}
