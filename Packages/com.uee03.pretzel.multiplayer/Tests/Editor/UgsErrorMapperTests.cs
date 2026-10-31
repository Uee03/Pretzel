using System;
using NUnit.Framework;
using Ugs = Unity.Services.Multiplayer;

namespace Pretzel.Multiplayer.Tests
{
    public sealed class UgsErrorMapperTests
    {
        [TestCase(Ugs.SessionError.SessionNotFound, MultiplayerErrorCode.SessionNotFound)]
        [TestCase(Ugs.SessionError.SessionDeleted, MultiplayerErrorCode.SessionEnded)]
        [TestCase(Ugs.SessionError.NotAuthorized, MultiplayerErrorCode.NotAuthorized)]
        [TestCase(Ugs.SessionError.Forbidden, MultiplayerErrorCode.NotAuthorized)]
        [TestCase(Ugs.SessionError.RateLimitExceeded, MultiplayerErrorCode.RateLimited)]
        [TestCase(Ugs.SessionError.InvalidParameter, MultiplayerErrorCode.InvalidRequest)]
        [TestCase(Ugs.SessionError.NetworkManagerNotInitialized, MultiplayerErrorCode.NetworkManagerMissing)]
        [TestCase(Ugs.SessionError.NetworkManagerStartFailed, MultiplayerErrorCode.ConnectionFailed)]
        [TestCase(Ugs.SessionError.NetworkSetupFailed, MultiplayerErrorCode.ConnectionFailed)]
        [TestCase(Ugs.SessionError.InvalidOperation, MultiplayerErrorCode.InvalidOperation)]
        public void Classify_MapsKnownSessionErrors(Ugs.SessionError error, MultiplayerErrorCode expected)
        {
            Assert.AreEqual(expected, UgsErrorMapper.Classify(error, "anything"));
        }

        [TestCase("The lobby is full", MultiplayerErrorCode.SessionFull)]
        [TestCase("Lobby is FULL.", MultiplayerErrorCode.SessionFull)]
        [TestCase("no open lobbies found", MultiplayerErrorCode.SessionNotFound)]
        [TestCase("Wrong password", MultiplayerErrorCode.NotAuthorized)]
        [TestCase("something else", MultiplayerErrorCode.Unknown)]
        [TestCase("", MultiplayerErrorCode.Unknown)]
        public void Classify_UnknownErrors_UseTheServiceText(string message, MultiplayerErrorCode expected)
        {
            Assert.AreEqual(expected, UgsErrorMapper.Classify(Ugs.SessionError.Unknown, message));
        }

        [Test]
        public void Map_SessionException_KeepsTheMessageAndTheCause()
        {
            Ugs.SessionException source = new Ugs.SessionException("session not found", Ugs.SessionError.SessionNotFound, null);

            MultiplayerException mapped = UgsErrorMapper.Map(source);

            Assert.AreEqual(MultiplayerErrorCode.SessionNotFound, mapped.Error.Code);
            Assert.AreEqual("session not found", mapped.Error.Message);
            Assert.AreSame(source, mapped.InnerException);
            Assert.IsFalse(mapped.Error.IsRetryable);
        }

        [Test]
        public void Map_MultiplayerException_PassesThrough()
        {
            MultiplayerException original = new MultiplayerException(MultiplayerErrorCode.Kicked, "kicked");

            Assert.AreSame(original, UgsErrorMapper.Map(original));
        }

        [Test]
        public void Map_OtherExceptions_BecomeUnknown()
        {
            MultiplayerException mapped = UgsErrorMapper.Map(new InvalidOperationException("boom"));

            Assert.AreEqual(MultiplayerErrorCode.Unknown, mapped.Error.Code);
            Assert.AreEqual("boom", mapped.Error.Message);
        }

        [Test]
        public void Map_Null_IsUnknown()
        {
            Assert.AreEqual(MultiplayerErrorCode.Unknown, UgsErrorMapper.Map(null).Error.Code);
        }
    }
}
