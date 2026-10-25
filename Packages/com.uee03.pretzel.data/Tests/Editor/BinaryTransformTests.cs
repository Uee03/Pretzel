using System;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Pretzel.Data.Editor;

namespace Pretzel.Data.Tests
{
    public sealed class BinaryTransformTests
    {
        [TestCase("{\"name\":\"Player\",\"level\":42,\"coins\":9223372036854775807,\"ratio\":1.25,\"active\":true,\"missing\":null}")]
        [TestCase("[1,\"two\",false,null,{},[3]]")]
        [TestCase("\"2026-10-20T12:30:00Z\"")]
        [TestCase("\"escaped \\\"text\\\" and \\u263a\"")]
        [TestCase("18446744073709551615")]
        [TestCase("null")]
        [TestCase("false")]
        [TestCase("-1.25e100")]
        public void BinaryValuesRoundTripWithoutBson(string json)
        {
            BinaryTransform transform = new();
            byte[] encoded = transform.Encode(Encoding.UTF8.GetBytes(json));
            Assert.That(BinaryTransform.HasHeader(encoded), Is.True);
            Assert.That(encoded[4], Is.EqualTo(1));
            string decoded = Encoding.UTF8.GetString(transform.Decode(encoded));
            Assert.That(JToken.DeepEquals(JToken.Parse(json), JToken.Parse(decoded)), Is.True, decoded);
            Assert.That(DataCodec.Detect(encoded), Is.EqualTo(DataFormat.Binary));
        }

        [Test]
        public void EveryTruncationIsRejected()
        {
            BinaryTransform transform = new();
            byte[] encoded = transform.Encode(Encoding.UTF8.GetBytes("{\"items\":[1,2,\"three\"]}"));
            for (int length = 0; length < encoded.Length; length++)
            {
                byte[] truncated = new byte[length];
                Array.Copy(encoded, truncated, length);
                Assert.That(() => transform.Decode(truncated),
                    Throws.InstanceOf<EndOfStreamException>().Or.InstanceOf<InvalidDataException>(), $"Length {length}");
            }
        }

        [Test]
        public void UnknownVersionTagsAndTrailingBytesAreRejected()
        {
            BinaryTransform transform = new();
            byte[] encoded = transform.Encode(Encoding.UTF8.GetBytes("null"));
            encoded[4] = 2;
            Assert.Throws<InvalidDataException>(() => transform.Decode(encoded));
            encoded[4] = 1;
            encoded[5] = 255;
            Assert.Throws<InvalidDataException>(() => transform.Decode(encoded));
            encoded[5] = 0;
            Array.Resize(ref encoded, encoded.Length + 1);
            Assert.Throws<InvalidDataException>(() => transform.Decode(encoded));
        }

        [Test]
        public void InvalidCollectionLengthsAreRejected()
        {
            byte[] data = { 80, 90, 66, 78, 1, 6, 255, 255, 255, 127 };
            Assert.Throws<InvalidDataException>(() => new BinaryTransform().Decode(data));
        }

        [Test]
        public void CodecUsesNewBinaryForEncoding()
        {
            CodecResult encoded = DataCodec.Encode("{\"level\":7}", DataFormat.Binary, EncryptionMode.Aes, false, null);
            Assert.That(encoded.Ok, Is.True, encoded.Message);
            Assert.That(BinaryTransform.HasHeader(Convert.FromBase64String(encoded.Text)), Is.True);
            CodecResult decoded = DataCodec.Decode(encoded.Text, null, null);
            Assert.That(decoded.Ok, Is.True, decoded.Message);
            Assert.That(JObject.Parse(decoded.Text)["level"].Value<int>(), Is.EqualTo(7));
        }
    }
}
