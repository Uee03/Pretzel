using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Pretzel.Multiplayer;
using SpaceInvaders.Coop;
using SpaceInvaders.Gameplay;
using UnityEngine;
using UnityEngine.TestTools;

namespace SpaceInvaders.Tests
{
    /// <summary>
    /// The host half of the two-process co-op test. It hosts a local session on loopback, waits for the client process to join, starts the
    /// match and checks that the remote player's shots reached the world. Run it together with <see cref="CoopRemoteClientTests"/>.
    /// </summary>
    [Explicit("Needs a second Unity process running CoopRemoteClientTests in another copy of the project. See BuildItYourself.md, Tests.")]
    public sealed class CoopRemoteHostTests
    {
        private PretzelNetworkBootstrap _bootstrap;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return CoopRemoteSupport.Leave(_bootstrap);
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator A_remote_client_joins_plays_and_scores_on_the_hosts_world()
        {
            CoopRemoteSupport.ClearMarkers();
            yield return CoopRemoteSupport.OpenCoopScene();
            _bootstrap = Object.FindAnyObjectByType<PretzelNetworkBootstrap>();
            CoopMatch match = Object.FindAnyObjectByType<CoopMatch>();
            InvaderFormation formation = Object.FindAnyObjectByType<InvaderFormation>();

            if (CoopRemoteSupport.Online)
            {
                CoopRemoteSupport.UseAuthenticationProfile(_bootstrap, "si-remote-host");
                Task<bool> hosting = _bootstrap.Service.HostOnlineAsync(new HostRequest { DisplayName = "Host", MaxPlayers = 2, SessionName = "SI remote test", IsPrivate = true });
                yield return CoopRemoteSupport.Wait(hosting, 90f);
                Assert.IsTrue(hosting.Result, "The online session starts.");
                CoopRemoteSupport.Mark(CoopRemoteSupport.HostReady, _bootstrap.Service.Lobby.JoinCode);
            }
            else
            {
                Task<bool> start = _bootstrap.Service.StartLocalAsync(new HostRequest { DisplayName = "Host", LocalPlayerCount = 1 });
                yield return CoopRemoteSupport.Wait(start);
                Assert.IsTrue(start.Result, "The local host starts.");
                CoopRemoteSupport.Mark(CoopRemoteSupport.HostReady);
            }

            yield return CoopRemoteSupport.Until(() => _bootstrap.NetworkManager.ConnectedClientsIds.Count >= 2, "the remote client to connect", 420f);
            yield return CoopRemoteSupport.Until(() => EveryGuestIsReady(_bootstrap.Service.Lobby), "the guest to be ready", 60f);
            Task<bool> begin = _bootstrap.Service.StartMatchAsync();
            yield return CoopRemoteSupport.Wait(begin);
            Assert.IsTrue(begin.Result, "The host starts the match.");
            yield return CoopRemoteSupport.Until(
                () => match.Ships.Count == 2 && match.Phase == CoopPhase.Playing && formation.AliveCount > 0,
                "both ships to spawn and the first wave to start",
                30f);
            CoopRemoteSupport.Mark(CoopRemoteSupport.MatchStarted);

            yield return CoopRemoteSupport.WaitForMarker(CoopRemoteSupport.ClientDone, 240f);
            string verdict = CoopRemoteSupport.Read(CoopRemoteSupport.ClientDone);
            Assert.IsTrue(verdict.StartsWith("PASS", System.StringComparison.Ordinal), "The client reported: " + verdict);

            CoopShip remote = null;
            foreach (CoopShip ship in match.Ships)
            {
                if (!ship.IsLocallyControlled)
                {
                    remote = ship;
                }
            }

            Assert.IsNotNull(remote, "The host sees the remote player's ship.");
            List<CoopPlayerStat> stats = new();
            match.CopyStats(stats);
            int remoteScore = -1;
            foreach (CoopPlayerStat stat in stats)
            {
                if (stat.Slot == remote.Slot)
                {
                    remoteScore = stat.Score;
                }
            }

            Assert.Greater(remoteScore, 0, "The remote player's shots scored on the host.");
        }

        // Local mode has no guests; an online session lets the host start once every guest has pressed Ready.
        private static bool EveryGuestIsReady(LobbySnapshot lobby)
        {
            if (lobby == null)
            {
                return false;
            }

            foreach (LobbyPlayerInfo player in lobby.Players)
            {
                if (!player.IsHost && !player.IsReady)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
