using System;
using NUnit.Framework;
using Pretzel.Data;

namespace Pretzel.Cloud.Tests
{
    public sealed class CloudKeyMapperTests
    {
        private static readonly string[] _allowed = { "-", "_" };

        [Test]
        public void PlainKeyMapsToTheDocumentedLayout()
        {
            Assert.That(CloudKeyMapper.ToCloudKey(new DataKey("profile")), Is.EqualTo("pz-n--profile"));
            Assert.That(CloudKeyMapper.ToCloudKey(new DataKey("save", 2, "Saves")), Is.EqualTo("pz-2-Saves-save"));
        }

        [TestCase("profile", null, null)]
        [TestCase("save", 0, "Saves")]
        [TestCase("save", 12, "Saves/Chapter1")]
        [TestCase("my file.v2", null, null)]
        [TestCase("slot-info", 3, "a-b/c_d")]
        [TestCase("ção", null, "pasta/açaí")]
        [TestCase("with#hash@at", 1, null)]
        public void RoundTripsAnyValidKey(string name, int? slot, string folder)
        {
            DataKey key = new(name, slot, folder);
            string cloudKey = CloudKeyMapper.ToCloudKey(key);
            Assert.That(CloudKeyMapper.TryParse(cloudKey, out DataKey parsed), Is.True, cloudKey);
            Assert.That(parsed, Is.EqualTo(key));
        }

        [Test]
        public void CloudKeysOnlyUseTheAllowedCharacters()
        {
            string cloudKey = CloudKeyMapper.ToCloudKey(new DataKey("a b.c_ção", 4, "x y/z"));
            foreach (char c in cloudKey)
            {
                bool allowed = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || Array.IndexOf(_allowed, c.ToString()) >= 0;
                Assert.That(allowed, Is.True, "'" + c + "' in " + cloudKey);
            }

            Assert.That(cloudKey.Length, Is.LessThanOrEqualTo(CloudKeyMapper.MaxKeyLength));
        }

        [Test]
        public void DifferentKeysNeverCollide()
        {
            string a = CloudKeyMapper.ToCloudKey(new DataKey("a-b"));
            string b = CloudKeyMapper.ToCloudKey(new DataKey("a_b"));
            string c = CloudKeyMapper.ToCloudKey(new DataKey("a", null, "b"));
            string d = CloudKeyMapper.ToCloudKey(new DataKey("b", null, "a"));
            string e = CloudKeyMapper.ToCloudKey(new DataKey("a", 1));
            Assert.That(new[] { a, b, c, d, e }, Is.Unique);
        }

        [Test]
        public void TooLongKeysAreRejected()
        {
            DataKey key = new(new string('é', 100));
            Assert.That(CloudKeyMapper.TryToCloudKey(key, out string cloudKey), Is.False);
            Assert.That(cloudKey, Is.Null);
            Assert.Throws<ArgumentException>(() => CloudKeyMapper.ToCloudKey(key));
        }

        [Test]
        public void UninitializedKeyIsRejected()
        {
            Assert.That(CloudKeyMapper.TryToCloudKey(default, out _), Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("profile")]
        [TestCase("pz-")]
        [TestCase("pz-n-profile")]
        [TestCase("pz-n--")]
        [TestCase("pz-x--profile")]
        [TestCase("pz--1--profile")]
        [TestCase("pz-n--pro_ZZ")]
        [TestCase("pz-n--pro_4")]
        [TestCase("pz-n--pro-file")]
        public void ForeignOrMalformedKeysAreNotParsed(string cloudKey)
        {
            Assert.That(CloudKeyMapper.TryParse(cloudKey, out _), Is.False);
        }
    }
}
