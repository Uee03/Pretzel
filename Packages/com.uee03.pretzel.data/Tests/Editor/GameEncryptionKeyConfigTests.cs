using System;
using System.IO;
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
        [Test]
        public void AesExportContainsOnlyAesKeyAndRoundTripsEncryptedData()
        {
            EncryptionKeys original = new()
            {
                AesKey = EncryptionKeys.GenerateAesKey()
            };
            string json = GameEncryptionKeyConfig.ToJson(original, EncryptionMode.Aes);
            JObject parsed = JObject.Parse(json);
            Assert.That(parsed.Count, Is.EqualTo(2), "Only the AES key and its fingerprint are exported.");
            Assert.That(parsed.ContainsKey("aesKey") && parsed.ContainsKey("keyId"), Is.True);
            EncryptionKeys restored = GameEncryptionKeyConfig.FromJson(json);
            Assert.That(restored.AesKey, Is.EqualTo(original.AesKey));

            byte[] data = { 1, 2, 3, 4 };
            EncryptionTransform writer = new(EncryptionMode.Aes, original);
            EncryptionTransform reader = new(EncryptionMode.Aes, restored);
            Assert.That(reader.Decode(writer.Encode(data)), Is.EqualTo(data));
        }

        [TestCase(EncryptionMode.Aes)]
        public void MissingKeyCannotBeExported(EncryptionMode mode)
        {
            Assert.Throws<InvalidOperationException>(() => GameEncryptionKeyConfig.ToJson(new EncryptionKeys(), mode));
        }

        [TestCase("{}")]
        [TestCase("{\"aesKey\":\"invalid\"}")]
        [TestCase("{\"aesKey\":\"AQIDBA==\"}")]
        [TestCase("{\"aesKey\":42}")]
        [TestCase("{\"aesKey\":null}")]
        [TestCase("{\"aesKey\":\"AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=\",\"keyId\":\"00:00:00:00:00:00\"}")]
        [TestCase("{\"aesKey\":\"AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=\",\"previousAesKeys\":[\"AQIDBA==\"]}")]
        [TestCase("{\"aesKey\":\"AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=\",\"previousAesKeys\":\"not a list\"}")]
        [TestCase("{\"aesKey\":\"AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=\",\"previousAesKeys\":[42]}")]
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
        public void UnsupportedModeCannotBeExported()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GameEncryptionKeyConfig.ToJson(
                new EncryptionKeys { AesKey = EncryptionKeys.GenerateAesKey() }, (EncryptionMode)1));
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
