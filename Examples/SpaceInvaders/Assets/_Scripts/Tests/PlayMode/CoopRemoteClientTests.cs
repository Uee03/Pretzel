using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Pretzel.Events;
using Pretzel.Multiplayer;
using SpaceInvaders.Coop;
using SpaceInvaders.Gameplay;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;
using Object = UnityEngine.Object;

namespace SpaceInvaders.Tests
{
    /// <summary>
    /// The client half of the two-process co-op test. It joins the host's loopback session as a genuine remote client, then checks what a client
    /// must see (the host's formation, ships and phase), shoots with its own ship, and checks that the kill comes back as replicated state,
    /// relayed events and a score. Run it together with <see cref="CoopRemoteHostTests"/>.
    /// </summary>
    [Explicit("Needs a second Unity process running CoopRemoteHostTests in another copy of the project. See BuildItYourself.md, Tests.")]
    public sealed class CoopRemoteClientTests
    {
        private PretzelNetworkBootstrap _bootstrap;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return CoopRemoteSupport.Leave(_bootstrap);
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator A_client_sees_the_hosts_world_and_its_own_shots_change_it()
        {
            string result = "FAIL: the client test did not finish";
            try
            {
                yield return CoopRemoteSupport.OpenCoopScene();
                _bootstrap = Object.FindAnyObjectByType<PretzelNetworkBootstrap>();
                CoopMatch match = Object.FindAnyObjectByType<CoopMatch>();
                InvaderFormation formation = Object.FindAnyObjectByType<InvaderFormation>();
                yield return CoopRemoteSupport.WaitForMarker(CoopRemoteSupport.HostReady, 420f);

                NetworkManager manager = _bootstrap.NetworkManager;
                if (CoopRemoteSupport.Online)
                {
                    CoopRemoteSupport.UseAuthenticationProfile(_bootstrap, "si-remote-client");
                    string code = CoopRemoteSupport.Read(CoopRemoteSupport.HostReady);
                    Task<bool> joining = _bootstrap.Service.JoinOnlineAsync(new JoinRequest { Kind = JoinKind.Code, Code = code, DisplayName = "Remote" });
                    yield return CoopRemoteSupport.Wait(joining, 90f);
                    Assert.IsTrue(joining.Result, "The client joins the online session by code.");
                    Task<bool> ready = _bootstrap.Service.SetReadyAsync(true);
                    yield return CoopRemoteSupport.Wait(ready);
                }
                else
                {
                    UnityTransport transport = (UnityTransport)manager.NetworkConfig.NetworkTransport;
                    transport.SetConnectionData("127.0.0.1", (ushort)_bootstrap.Settings.LoopbackPort);
                    manager.NetworkConfig.ConnectionData = ConnectionPayloadCodec.Encode(new ConnectionPayload("remote-player", "Remote", "windows", 1), 512);
                    Assert.IsTrue(manager.StartClient(), "The client starts.");
                }

                yield return CoopRemoteSupport.Until(() => manager.IsConnectedClient && match.IsSpawned, "the client to connect and sync the match", 60f);
                Assert.IsFalse(manager.IsServer, "This process is a client.");
                Assert.IsNotNull(Object.FindAnyObjectByType<CoopMatch>(), "The scene's match object survived the join.");

                int destroyedEvents = 0;
                Root().Container.Resolve<IEventBus>().Subscribe<InvaderDestroyed>(_ => destroyedEvents++);

                yield return CoopRemoteSupport.WaitForMarker(CoopRemoteSupport.MatchStarted, 120f);
                yield return CoopRemoteSupport.Until(
                    () => match.Phase == CoopPhase.Playing && formation.AliveCount > 0 && match.Ships.Count == 2,
                    "the host's match, formation and both ships to reach the client",
                    30f);
                Assert.IsFalse(formation.IsRunning, "The client's formation only mirrors: it never marches.");
                Assert.AreEqual(55, formation.AliveCount, "The client built the host's full first wave.");
                Assert.AreEqual(1, match.Wave);

                CoopShip mine = null;
                foreach (CoopShip ship in match.Ships)
                {
                    if (ship.IsLocallyControlled)
                    {
                        mine = ship;
                    }
                }

                Assert.IsNotNull(mine, "The client controls one of the two ships.");
                int startingAlive = formation.AliveCount;
                Invader target = CoopRemoteSupport.LowestInColumn(formation, 5);
                mine.transform.position = new Vector3(target.transform.position.x, mine.transform.position.y, 0f);
                mine.Input = new HeldFireInput();

                yield return CoopRemoteSupport.Until(() => formation.AliveCount < startingAlive, "the client's shot to kill an invader on the host and show up here", 30f);
                mine.Input = null;
                yield return new WaitForSeconds(1f);

                Assert.Greater(destroyedEvents, 0, "The host's InvaderDestroyed event was relayed to this client's bus.");
                List<CoopPlayerStat> stats = new();
                match.CopyStats(stats);
                int myScore = -1;
                foreach (CoopPlayerStat stat in stats)
                {
                    if (stat.Slot == mine.Slot)
                    {
                        myScore = stat.Score;
                    }
                }

                Assert.Greater(myScore, 0, "The score replicated back to the shooter.");
                result = "PASS";
            }
            finally
            {
                CoopRemoteSupport.Mark(CoopRemoteSupport.ClientDone, result);
            }
        }

        private static VContainer.Unity.LifetimeScope Root()
        {
            return CoopRemoteSupport.Root();
        }

        private sealed class HeldFireInput : ICoopShipInput
        {
            public float ReadHorizontal(int localPlayerIndex, bool acceptEveryScheme)
            {
                return 0f;
            }

            public bool ReadFire(int localPlayerIndex, bool acceptEveryScheme)
            {
                return true;
            }
        }
    }
}
