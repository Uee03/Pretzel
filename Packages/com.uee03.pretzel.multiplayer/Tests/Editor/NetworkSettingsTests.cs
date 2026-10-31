using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Pretzel.Multiplayer.Tests
{
    public sealed class NetworkSettingsTests
    {
        private static NetworkSettings FromJson(string json)
        {
            NetworkSettings settings = new NetworkSettings();
            JsonUtility.FromJsonOverwrite(json, settings);
            return settings;
        }

        [Test]
        public void Defaults_AreValid()
        {
            Assert.IsEmpty(new NetworkSettings().Validate());
        }

        [Test]
        public void Defaults_AreSmallCoopFriendly()
        {
            NetworkSettings settings = new NetworkSettings();

            Assert.AreEqual(30, settings.TickRate);
            Assert.GreaterOrEqual(settings.MaxLocalPlayersPerClient, 2, "Couch co-op needs at least two local players");
            Assert.AreEqual(settings.MaxPlayers * settings.MaxLocalPlayersPerClient, settings.MaxPlayerObjects);
            Assert.AreEqual(PlayerSpawnTiming.OnMatchStart, settings.SpawnTiming);
        }

        [Test]
        public void TickRateOutsideTheSupportedRange_IsReported()
        {
            Assert.IsTrue(FromJson("{\"_tickRate\":5}").Validate().Any(p => p.Contains("Tick rate", System.StringComparison.Ordinal)));
            Assert.IsTrue(FromJson("{\"_tickRate\":500}").Validate().Any(p => p.Contains("Tick rate", System.StringComparison.Ordinal)));
        }

        [Test]
        public void NonPositiveLimits_AreReported()
        {
            NetworkSettings settings = FromJson("{\"_maxPlayers\":0,\"_maxLocalPlayersPerClient\":0,\"_maxConnectionDataBytes\":10,\"_approvalTimeoutSeconds\":0}");

            System.Collections.Generic.IReadOnlyList<string> problems = settings.Validate();

            Assert.AreEqual(4, problems.Count, string.Join(" | ", problems));
        }

        [Test]
        public void ReconnectDelays_MustBePositiveAndOrdered()
        {
            Assert.IsNotEmpty(FromJson("{\"_reconnectBaseDelaySeconds\":0}").Validate());
            Assert.IsNotEmpty(FromJson("{\"_reconnectBaseDelaySeconds\":5,\"_reconnectMaxDelaySeconds\":1}").Validate());
        }

        [Test]
        public void LoopbackPort_MustBeUnprivileged()
        {
            Assert.IsNotEmpty(FromJson("{\"_loopbackPort\":80}").Validate());
            Assert.IsNotEmpty(FromJson("{\"_loopbackPort\":70000}").Validate());
            Assert.IsEmpty(FromJson("{\"_loopbackPort\":7777}").Validate());
        }

        [TestCase("", true)]
        [TestCase("player-2_b", true)]
        [TestCase("has space", false)]
        [TestCase("bad!char", false)]
        [TestCase("123456789012345678901234567890x", false)]
        public void AuthenticationProfile_IsValidated(string profile, bool valid)
        {
            Assert.AreEqual(valid, NetworkSettings.IsValidProfile(profile));
            NetworkSettings settings = FromJson("{\"_authenticationProfile\":\"" + profile + "\"}");
            Assert.AreEqual(valid, settings.Validate().Count == 0);
        }

        [Test]
        public void Properties_ExposeTheSerializedValues()
        {
            NetworkSettings settings = FromJson("{\"_tickRate\":60,\"_maxPlayers\":8,\"_reconnectGraceSeconds\":30,\"_reconnectMaxAttempts\":2,\"_protocolVersion\":3}");

            Assert.AreEqual(60, settings.TickRate);
            Assert.AreEqual(8, settings.MaxPlayers);
            Assert.AreEqual(30d, settings.ReconnectGrace.TotalSeconds);
            Assert.AreEqual(2, settings.ReconnectMaxAttempts);
            Assert.AreEqual(3, settings.ProtocolVersion);
        }
    }
}
