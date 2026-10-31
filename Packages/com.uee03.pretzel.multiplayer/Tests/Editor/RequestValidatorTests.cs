using NUnit.Framework;

namespace Pretzel.Multiplayer.Tests
{
    public sealed class RequestValidatorTests
    {
        private static NetworkSettings Settings { get; } = new NetworkSettings();

        [Test]
        public void ValidLocalHost_Passes()
        {
            Assert.IsNull(RequestValidator.ValidateHost(SessionMode.Local, new HostRequest { LocalPlayerCount = 2 }, Settings));
        }

        [Test]
        public void LocalPlayerCount_MustBeWithinTheConfiguredRange()
        {
            Assert.AreEqual(MultiplayerErrorCode.InvalidRequest, RequestValidator.ValidateHost(SessionMode.Local, new HostRequest { LocalPlayerCount = 0 }, Settings).Code);
            Assert.AreEqual(MultiplayerErrorCode.InvalidRequest,
                RequestValidator.ValidateHost(SessionMode.Local, new HostRequest { LocalPlayerCount = Settings.MaxLocalPlayersPerClient + 1 }, Settings).Code);
        }

        [Test]
        public void OnlineHost_ChecksMaxPlayersAndName()
        {
            Assert.IsNull(RequestValidator.ValidateHost(SessionMode.Online, new HostRequest { MaxPlayers = 4, SessionName = "Room" }, Settings));
            Assert.IsNotNull(RequestValidator.ValidateHost(SessionMode.Online, new HostRequest { MaxPlayers = 0, SessionName = "Room" }, Settings));
            Assert.IsNotNull(RequestValidator.ValidateHost(SessionMode.Online, new HostRequest { MaxPlayers = Settings.MaxPlayers + 1, SessionName = "Room" }, Settings));
            Assert.IsNotNull(RequestValidator.ValidateHost(SessionMode.Online, new HostRequest { MaxPlayers = 2, SessionName = "  " }, Settings));
            Assert.IsNotNull(RequestValidator.ValidateHost(SessionMode.Online, new HostRequest { MaxPlayers = 2, SessionName = new string('x', 65) }, Settings));
        }

        [Test]
        public void LocalHost_IgnoresOnlineOnlyFields()
        {
            Assert.IsNull(RequestValidator.ValidateHost(SessionMode.Local, new HostRequest { MaxPlayers = 0, SessionName = string.Empty }, Settings));
        }

        [Test]
        public void JoinByCode_NeedsACode()
        {
            Assert.IsNull(RequestValidator.ValidateJoin(new JoinRequest { Kind = JoinKind.Code, Code = "ABC123" }, Settings));
            Assert.IsNotNull(RequestValidator.ValidateJoin(new JoinRequest { Kind = JoinKind.Code, Code = " " }, Settings));
            Assert.IsNotNull(RequestValidator.ValidateJoin(new JoinRequest { Kind = JoinKind.Code, Code = null }, Settings));
        }

        [Test]
        public void JoinById_NeedsAnId_AndQuickJoinNeedsNothing()
        {
            Assert.IsNotNull(RequestValidator.ValidateJoin(new JoinRequest { Kind = JoinKind.SessionId }, Settings));
            Assert.IsNull(RequestValidator.ValidateJoin(new JoinRequest { Kind = JoinKind.SessionId, SessionId = "id" }, Settings));
            Assert.IsNull(RequestValidator.ValidateJoin(new JoinRequest { Kind = JoinKind.QuickJoin }, Settings));
        }

        [Test]
        public void MissingRequest_IsInvalid()
        {
            Assert.AreEqual(MultiplayerErrorCode.InvalidRequest, RequestValidator.ValidateJoin(null, Settings).Code);
        }

        [TestCase("  Ada  ", "Ada")]
        [TestCase("", "Player")]
        [TestCase(null, "Player")]
        public void NormalizeName_TrimsAndFallsBack(string input, string expected)
        {
            Assert.AreEqual(expected, RequestValidator.NormalizeName(input));
        }

        [Test]
        public void NormalizeName_CapsTheLength()
        {
            Assert.AreEqual(RequestValidator.MaxNameLength, RequestValidator.NormalizeName(new string('a', 100)).Length);
        }
    }
}
