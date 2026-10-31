using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace Pretzel.Multiplayer.Tests
{
    public sealed class ConnectionPayloadCodecTests
    {
        [Test]
        public void EncodeThenDecode_RoundTrips()
        {
            ConnectionPayload payload = new ConnectionPayload("player-1", "Ada", "windows", 2);

            byte[] bytes = ConnectionPayloadCodec.Encode(payload, 512);
            bool ok = ConnectionPayloadCodec.TryDecode(bytes, 512, 4, out ConnectionPayload decoded, out string error);

            Assert.IsTrue(ok, error);
            Assert.AreEqual("player-1", decoded.PlayerKey);
            Assert.AreEqual("Ada", decoded.DisplayName);
            Assert.AreEqual("windows", decoded.PlatformTag);
            Assert.AreEqual(2, decoded.LocalPlayerCount);
        }

        [Test]
        public void Encode_ThrowsWhenLargerThanTheLimit()
        {
            ConnectionPayload payload = new ConnectionPayload("player-1", new string('x', 600), "windows", 1);

            Assert.Throws<System.InvalidOperationException>(() => ConnectionPayloadCodec.Encode(payload, 512));
        }

        [Test]
        public void TryDecode_RejectsOversizedDataBeforeParsing()
        {
            byte[] big = new byte[2000];

            Assert.IsFalse(ConnectionPayloadCodec.TryDecode(big, 512, 4, out ConnectionPayload payload, out string error));
            Assert.IsNull(payload);
            Assert.AreEqual("Connection data is too large.", error);
        }

        [Test]
        public void TryDecode_RejectsMissingAndGarbageData()
        {
            Assert.IsFalse(ConnectionPayloadCodec.TryDecode(null, 512, 4, out _, out _));
            Assert.IsFalse(ConnectionPayloadCodec.TryDecode(new byte[0], 512, 4, out _, out _));
            Assert.IsFalse(ConnectionPayloadCodec.TryDecode(Encoding.UTF8.GetBytes("not json"), 512, 4, out _, out string error));
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void TryDecode_RejectsAnUnknownVersion()
        {
            string json = JsonUtility.ToJson(new ConnectionPayload("p", "n", "t", 1)).Replace("\"_version\":1", "\"_version\":99");

            Assert.IsFalse(ConnectionPayloadCodec.TryDecode(Encoding.UTF8.GetBytes(json), 512, 4, out _, out string error));
            StringAssert.Contains("version", error);
        }

        [Test]
        public void TryDecode_RejectsEmptyOrOverlongKeys()
        {
            Assert.IsFalse(ConnectionPayloadCodec.TryDecode(ConnectionPayloadCodec.Encode(new ConnectionPayload(string.Empty, "n", "t", 1), 512), 512, 4, out _, out _));
            Assert.IsFalse(ConnectionPayloadCodec.TryDecode(ConnectionPayloadCodec.Encode(new ConnectionPayload(new string('k', 65), "n", "t", 1), 512), 512, 4, out _, out _));
        }

        [Test]
        public void TryDecode_RejectsTooManyLocalPlayers()
        {
            byte[] bytes = ConnectionPayloadCodec.Encode(new ConnectionPayload("p", "n", "t", 5), 512);

            Assert.IsFalse(ConnectionPayloadCodec.TryDecode(bytes, 512, 4, out _, out string error));
            Assert.AreEqual("Too many local players.", error);
        }
    }
}
