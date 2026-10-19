using System;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Data.Tests
{
    public sealed class GameEncryptionKeyConfigTests
    {
        private EncryptionKeys _rsa;

        [OneTimeSetUp]
        public void CreateRsaKeys()
        {
            _rsa = EncryptionKeys.GenerateRsa(2048);
        }

        [Test]
        public void AesExportContainsOnlyAesKeyAndRoundTripsEncryptedData()
        {
            EncryptionKeys original = new()
            {
                AesKey = EncryptionKeys.GenerateAesKey(),
                RsaPublicKey = _rsa.RsaPublicKey,
                RsaPrivateKey = _rsa.RsaPrivateKey
            };
            string json = GameEncryptionKeyConfig.ToJson(original, EncryptionMode.Aes);
            Assert.That(JObject.Parse(json).Count, Is.EqualTo(1));
            EncryptionKeys restored = GameEncryptionKeyConfig.FromJson(json);
            Assert.That(restored.AesKey, Is.EqualTo(original.AesKey));
            Assert.That(restored.HasPublicKey || restored.HasPrivateKey, Is.False);

            byte[] data = { 1, 2, 3, 4 };
            EncryptionTransform writer = new(EncryptionMode.Aes, original);
            EncryptionTransform reader = new(EncryptionMode.Aes, restored);
            Assert.That(reader.Decode(writer.Encode(data)), Is.EqualTo(data));
        }

        [Test]
        public void RsaExportContainsOnlyPublicKeyAndCanEncryptForServer()
        {
            EncryptionKeys original = new()
            {
                AesKey = EncryptionKeys.GenerateAesKey(),
                RsaPublicKey = _rsa.RsaPublicKey,
                RsaPrivateKey = _rsa.RsaPrivateKey
            };
            string json = GameEncryptionKeyConfig.ToJson(original, EncryptionMode.Rsa);
            Assert.That(JObject.Parse(json).Count, Is.EqualTo(1));
            EncryptionKeys restored = GameEncryptionKeyConfig.FromJson(json);
            Assert.That(restored.RsaPublicKey, Is.EqualTo(_rsa.RsaPublicKey));
            Assert.That(restored.HasAesKey || restored.HasPrivateKey, Is.False);

            byte[] data = { 5, 6, 7, 8 };
            EncryptionTransform writer = new(EncryptionMode.Rsa, restored);
            EncryptionTransform server = new(EncryptionMode.Rsa, _rsa);
            Assert.That(server.Decode(writer.Encode(data)), Is.EqualTo(data));
        }

        [Test]
        public void RsaExportStripsPrivateParametersEvenFromPublicKeyField()
        {
            string json = GameEncryptionKeyConfig.ToJson(
                new EncryptionKeys { RsaPublicKey = _rsa.RsaPrivateKey }, EncryptionMode.Rsa);
            Assert.That(GameEncryptionKeyConfig.FromJson(json).RsaPublicKey, Is.EqualTo(_rsa.RsaPublicKey));
            Assert.That(json, Does.Not.Contain("rsaPrivateKey"));
        }

        [TestCase(EncryptionMode.Aes)]
        [TestCase(EncryptionMode.Rsa)]
        public void MissingKeyCannotBeExported(EncryptionMode mode)
        {
            Assert.Throws<InvalidOperationException>(() => GameEncryptionKeyConfig.ToJson(new EncryptionKeys(), mode));
        }

        [TestCase("{}")]
        [TestCase("{\"aesKey\":\"invalid\"}")]
        [TestCase("{\"aesKey\":\"AQIDBA==\"}")]
        [TestCase("{\"aesKey\":42}")]
        [TestCase("{\"aesKey\":null}")]
        [TestCase("{\"rsaPublicKey\":\"invalid\"}")]
        [TestCase("{\"rsaPrivateKey\":\"never ship this\"}")]
        public void InvalidConfigThrowsInsteadOfReturningMissingKeys(string json)
        {
            Assert.Throws<FormatException>(() => GameEncryptionKeyConfig.FromJson(json));
        }

        [Test]
        public void MalformedJsonThrows()
        {
            Assert.Throws<JsonReaderException>(() => GameEncryptionKeyConfig.FromJson("{"));
        }

        [Test]
        public void InvalidRsaXmlThrows()
        {
            Assert.Throws<CryptographicException>(() => GameEncryptionKeyConfig.FromJson("{\"rsaPublicKey\":\"<RSAKeyValue/>\"}"));
        }

        [Test]
        public void KeysTabImportsWithGameExportControls()
        {
            VisualTreeAsset layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.uee03.pretzel.data/Editor/Layouts/KeysTab.uxml");
            Assert.That(layout, Is.Not.Null);
            TemplateContainer view = layout.Instantiate();
            Assert.That(view.Q<Button>("keys-panel__game-json"), Is.Not.Null);
            Assert.That(view.Q<Button>("keys-panel__game-embedded"), Is.Not.Null);
            Assert.That(view.Q<Button>("keys-panel__game-select"), Is.Not.Null);
            Assert.That(view.Q<Label>("keys-panel__game-result"), Is.Not.Null);
        }

        [Test]
        public void ResourcesConfigIsResolvedAndExplicitSourcesKeepPrecedence()
        {
            if (Resources.Load<TextAsset>(GameEncryptionKeyConfig.ResourcePath) || EncryptionKeyFile.Exists)
            {
                Assert.Ignore("This project already has keys; do not replace user key material.");
            }

            string folder = "Assets/PretzelKeyConfigTest_" + Guid.NewGuid().ToString("N");
            string path = folder + "/Resources/" + GameEncryptionKeyConfig.ResourcePath + ".json";
            string prefix = "PRETZEL_TEST_" + Guid.NewGuid().ToString("N");
            Func<EncryptionKeys> previous = EncryptionKeys.Provider;
            EncryptionKeys bundled = new() { AesKey = EncryptionKeys.GenerateAesKey() };
            EncryptionKeys supplied = new() { AesKey = EncryptionKeys.GenerateAesKey() };
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, GameEncryptionKeyConfig.ToJson(bundled, EncryptionMode.Aes));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                EncryptionKeys.Provider = null;
                Assert.That(GameEncryptionKeyConfig.Load().AesKey, Is.EqualTo(bundled.AesKey));
                Assert.That(EncryptionKeys.Resolve(prefix).AesKey, Is.EqualTo(bundled.AesKey));
                DataIOConfig config = ScriptableObject.CreateInstance<DataIOConfig>();
                try
                {
                    config.format = DataFormat.Encrypted;
                    config.environmentPrefix = prefix;
                    Assert.That(config.CreateStore(), Is.Not.Null);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(config);
                }

                Environment.SetEnvironmentVariable(EncryptionKeys.AesVariable(prefix), EncryptionKeys.EncodeAesKey(supplied.AesKey));
                Assert.That(EncryptionKeys.Resolve(prefix).AesKey, Is.EqualTo(supplied.AesKey));

                EncryptionKeys.Provider = () => bundled;
                Assert.That(EncryptionKeys.Resolve(prefix).AesKey, Is.EqualTo(bundled.AesKey));
            }
            finally
            {
                EncryptionKeys.Provider = previous;
                Environment.SetEnvironmentVariable(EncryptionKeys.AesVariable(prefix), null);
                AssetDatabase.DeleteAsset(folder);
            }
        }
    }
}
