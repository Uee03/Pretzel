using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Pretzel.Multiplayer;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Pretzel.TestPretzel.LiveServicesTests
{
    /// <summary>
    /// Hosts a real online session (Authentication, Sessions and Relay) from this process. A second client cannot join from the same process
    /// because the services are per-process singletons; use Multiplayer Play Mode for that.
    /// </summary>
    [Category(LiveServices.Category)]
    public sealed class MultiplayerLiveTests
    {
        private GameObject _root;
        private NetworkManager _manager;
        private PretzelNetworkBootstrap _bootstrap;
        private CancellationTokenSource _timeout;
        private readonly List<MultiplayerError> _errors = new List<MultiplayerError>();

        [SetUp]
        public void SetUp()
        {
            LiveServices.RequireLinkedProject();
            _timeout = new CancellationTokenSource(LiveServices.Timeout);

            // Inactive while configured so the bootstrap's Awake sees the serialized fields.
            _root = new GameObject("NetworkManager");
            _root.SetActive(false);
            _manager = _root.AddComponent<NetworkManager>();
            UnityTransport transport = _root.AddComponent<UnityTransport>();
            _manager.NetworkConfig = new NetworkConfig { NetworkTransport = transport };
            _bootstrap = _root.AddComponent<PretzelNetworkBootstrap>();
#if UNITY_EDITOR
            UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(_bootstrap);
            serialized.FindProperty("_offerRejoinOnStart").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
            _root.SetActive(true);
            _bootstrap.Service.ErrorRaised += _errors.Add;
        }

        [TearDown]
        public async Task TearDownAsync()
        {
            if (_bootstrap != null && _bootstrap.Service != null)
            {
                _bootstrap.Service.ErrorRaised -= _errors.Add;
                if (_bootstrap.Service.State != ConnectionState.Disconnected)
                {
                    await _bootstrap.Service.LeaveAsync();
                }
            }

            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
            }

            _timeout?.Dispose();
        }

        [Test]
        public async Task HostOnline_CreatesARelaySession_ThatCanBeFoundReadiedStartedAndLeftAsync()
        {
            IMultiplayerSessionService service = _bootstrap.Service;
            string sessionName = "Pretzel live " + Guid.NewGuid().ToString("N").Substring(0, 8);

            bool hosted = await service.HostOnlineAsync(new HostRequest { SessionName = sessionName, DisplayName = "LiveHost", MaxPlayers = 2 }, _timeout.Token);

            Assert.IsTrue(hosted, Describe());
            Assert.AreEqual(ConnectionState.InLobby, service.State);
            Assert.IsTrue(_manager.IsHost, "NGO runs as host over Relay");
            LobbySnapshot lobby = service.Lobby;
            Assert.AreEqual(SessionMode.Online, lobby.Mode);
            Assert.IsTrue(lobby.LocalIsHost);
            Assert.IsFalse(string.IsNullOrEmpty(lobby.SessionId));
            Assert.IsFalse(string.IsNullOrEmpty(lobby.JoinCode), "A public session has a join code");
            Assert.AreEqual(1, lobby.Players.Count);
            Debug.Log($"[LiveServices] Hosted session {lobby.SessionId} '{lobby.SessionName}' with join code {lobby.JoinCode}.");

            SessionListing listing = await FindSessionAsync(service, lobby.SessionId);
            Assert.IsNotNull(listing, "The new session shows up in the session query");
            Assert.AreEqual(1, listing.PlayerCount);
            Assert.AreEqual(2, listing.MaxPlayers);

            Assert.IsTrue(await service.SetReadyAsync(true, _timeout.Token), Describe());
            Assert.IsTrue(await service.StartMatchAsync(_timeout.Token), Describe());
            Assert.AreEqual(ConnectionState.Playing, service.State);
            Assert.IsTrue(service.Lobby.MatchStarted);

            Assert.IsTrue(await service.EndMatchAsync(_timeout.Token), Describe());
            Assert.AreEqual(ConnectionState.InLobby, service.State);

            await service.LeaveAsync();
            Assert.AreEqual(ConnectionState.Disconnected, service.State);
            Assert.IsFalse(_manager.IsListening);
            Assert.IsNull(await FindSessionAsync(service, lobby.SessionId, 1), "Leaving as host deletes the session");
            CollectionAssert.IsEmpty(_errors, Describe());
        }

        private async Task<SessionListing> FindSessionAsync(IMultiplayerSessionService service, string sessionId, int attempts = 5)
        {
            // The session index can lag a moment behind creation.
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                IReadOnlyList<SessionListing> sessions = await service.QuerySessionsAsync(new SessionQuery { Count = 100 }, _timeout.Token);
                foreach (SessionListing session in sessions)
                {
                    if (string.Equals(session.Id, sessionId, StringComparison.Ordinal))
                    {
                        return session;
                    }
                }

                await Awaitable.WaitForSecondsAsync(1f, _timeout.Token);
            }

            return null;
        }

        private string Describe()
        {
            return _errors.Count == 0 ? "No error was raised." : string.Join(" | ", _errors);
        }
    }
}
