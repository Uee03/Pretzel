using NUnit.Framework;

namespace Pretzel.Multiplayer.Tests
{
    public sealed class LobbyRulesTests
    {
        [Test]
        public void Host_CanStart_WhenEveryOtherPlayerIsReady()
        {
            Assert.IsTrue(LobbyRules.CanStart(TestLobbies.Online(true, false, true)));
        }

        [Test]
        public void Host_CannotStart_WhileAGuestIsNotReady()
        {
            Assert.IsFalse(LobbyRules.CanStart(TestLobbies.Online(true, false, false)));
        }

        [Test]
        public void Client_CanNeverStart()
        {
            Assert.IsFalse(LobbyRules.CanStart(TestLobbies.Online(false, false, true)));
        }

        [Test]
        public void NoStart_OnceTheMatchIsRunning()
        {
            Assert.IsFalse(LobbyRules.CanStart(TestLobbies.Online(true, true, true)));
        }

        [Test]
        public void LocalMode_CanAlwaysStart()
        {
            Assert.IsTrue(LobbyRules.CanStart(TestLobbies.Local(2)));
        }

        [Test]
        public void NullLobby_CannotStart()
        {
            Assert.IsFalse(LobbyRules.CanStart(null));
        }

        [Test]
        public void IsLocalReady_ReadsTheLocalPlayersFlag()
        {
            Assert.IsTrue(LobbyRules.IsLocalReady(TestLobbies.Online(false, false, true)));
            Assert.IsFalse(LobbyRules.IsLocalReady(TestLobbies.Online(false, false, false)));
            Assert.IsFalse(LobbyRules.IsLocalReady(null));
        }
    }
}
