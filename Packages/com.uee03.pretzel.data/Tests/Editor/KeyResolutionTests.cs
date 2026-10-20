using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Pretzel.Data.Tests
{
    public sealed class KeyResolutionTests
    {
        private Func<EncryptionKeys> _previousProvider;
        private readonly List<string> _folders = new();
        private readonly List<string> _variables = new();

        [SetUp]
        public void SetUp()
        {
            _previousProvider = EncryptionKeys.Provider;
            EncryptionKeys.Provider = null;
        }

        [TearDown]
        public void TearDown()
        {
            EncryptionKeys.Provider = _previousProvider;
            foreach (string variable in _variables)
            {
                Environment.SetEnvironmentVariable(variable, null);
            }

            foreach (string folder in _folders)
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }

        private static EncryptionKeys NewAes() => new() { AesKey = EncryptionKeys.GenerateAesKey() };

        private string NewPrefix()
        {
            string prefix = "PRETZEL_TEST_" + Guid.NewGuid().ToString("N");
            _variables.Add(EncryptionKeys.AesVariable(prefix));
            return prefix;
        }

        private void BundleJson(EncryptionKeys keys)
        {
            if (Resources.Load<TextAsset>(GameEncryptionKeyConfig.ResourcePath))
            {
                Assert.Ignore("This project already bundles keys; the test will not replace them.");
            }

            string folder = "Assets/PretzelKeyResolution_" + Guid.NewGuid().ToString("N");
            _folders.Add(folder);
            string path = folder + "/Resources/" + GameEncryptionKeyConfig.ResourcePath + ".json";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, GameEncryptionKeyConfig.ToJson(keys, EncryptionMode.Aes));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        [Test]
        public void ProviderSourceIgnoresEnvironmentVariables()
        {
            string prefix = NewPrefix();
            EncryptionKeys fromEnvironment = NewAes();
            EncryptionKeys fromProvider = NewAes();
            Environment.SetEnvironmentVariable(EncryptionKeys.AesVariable(prefix), EncryptionKeys.EncodeAesKey(fromEnvironment.AesKey));

            Assert.That(EncryptionKeys.Resolve(prefix, KeySource.Provider, out string origin), Is.Null);
            Assert.That(origin, Is.EqualTo("none"));

            EncryptionKeys.Provider = () => fromProvider;
            EncryptionKeys resolved = EncryptionKeys.Resolve(prefix, KeySource.Provider, out origin);
            Assert.That(resolved.AesKey, Is.EqualTo(fromProvider.AesKey));
            Assert.That(origin, Does.StartWith("Provider"));
        }

        [Test]
        public void EnvironmentSourceIgnoresTheProvider()
        {
            string prefix = NewPrefix();
            EncryptionKeys fromEnvironment = NewAes();
            EncryptionKeys.Provider = NewAes;
            Environment.SetEnvironmentVariable(EncryptionKeys.AesVariable(prefix), EncryptionKeys.EncodeAesKey(fromEnvironment.AesKey));

            EncryptionKeys resolved = EncryptionKeys.Resolve(prefix, KeySource.Environment, out string origin);
            Assert.That(resolved.AesKey, Is.EqualTo(fromEnvironment.AesKey));
            Assert.That(origin, Is.EqualTo("environment variables"));
        }

        [Test]
        public void BundledJsonSourceIgnoresProviderAndEnvironment()
        {
            string prefix = NewPrefix();
            EncryptionKeys bundled = NewAes();
            BundleJson(bundled);
            Environment.SetEnvironmentVariable(EncryptionKeys.AesVariable(prefix), EncryptionKeys.EncodeAesKey(NewAes().AesKey));
            EncryptionKeys.Provider = NewAes;

            EncryptionKeys resolved = EncryptionKeys.Resolve(prefix, KeySource.BundledJson, out string origin);
            Assert.That(resolved.AesKey, Is.EqualTo(bundled.AesKey));
            Assert.That(origin, Is.EqualTo("bundled JSON config"));
        }

        [Test]
        public void AutomaticKeepsTheDocumentedOrderAndNamesEverySource()
        {
            string prefix = NewPrefix();
            EncryptionKeys provider = NewAes();
            EncryptionKeys.Provider = () => provider;
            Environment.SetEnvironmentVariable(EncryptionKeys.AesVariable(prefix), EncryptionKeys.EncodeAesKey(NewAes().AesKey));

            EncryptionKeys resolved = EncryptionKeys.Resolve(prefix, KeySource.Automatic, out string origin);
            Assert.That(resolved.AesKey, Is.EqualTo(provider.AesKey), "The provider wins.");
            Assert.That(origin, Does.StartWith("Provider"));
            Assert.That(EncryptionKeys.Resolve(prefix).AesKey, Is.EqualTo(provider.AesKey));
        }

        [Test]
        public void ConfigCreateStoreFollowsItsKeySource()
        {
            DataIOConfig config = ScriptableObject.CreateInstance<DataIOConfig>();
            try
            {
                config.format = DataFormat.Encrypted;
                config.environmentPrefix = NewPrefix();
                Environment.SetEnvironmentVariable(EncryptionKeys.AesVariable(config.environmentPrefix),
                    EncryptionKeys.EncodeAesKey(NewAes().AesKey));

                config.keySource = KeySource.Provider;
                InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => config.CreateStore());
                Assert.That(error.Message, Does.Contain("Provider"));

                config.keySource = KeySource.Environment;
                Assert.That(config.CreateStore(), Is.Not.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void ForClientNeverIncludesThePrivateKey()
        {
            EncryptionKeys pair = EncryptionKeys.GenerateRsa(2048);
            pair.AesKey = EncryptionKeys.GenerateAesKey();
            pair.AddPreviousAesKey(EncryptionKeys.GenerateAesKey());

            EncryptionKeys rsa = pair.ForClient(EncryptionMode.Rsa);
            Assert.That(rsa.HasPublicKey, Is.True);
            Assert.That(rsa.HasPrivateKey || rsa.HasAesKey, Is.False);

            EncryptionKeys aes = pair.ForClient(EncryptionMode.Aes);
            Assert.That(aes.HasAesKey, Is.True);
            Assert.That(aes.PreviousAesKeys, Has.Count.EqualTo(1));
            Assert.That(aes.HasPublicKey || aes.HasPrivateKey, Is.False);
            Assert.That(pair.CanWrite(EncryptionMode.Rsa) && pair.CanWrite(EncryptionMode.Aes), Is.True);
            Assert.That(new EncryptionKeys().CanWrite(EncryptionMode.Aes), Is.False);
        }

        [Test]
        public void DescribeShowsFingerprintsOnly()
        {
            EncryptionKeys keys = NewAes();
            keys.AddPreviousAesKey(EncryptionKeys.GenerateAesKey());
            string text = keys.Describe(EncryptionMode.Aes, "bundled JSON config");

            Assert.That(text, Does.Contain(EncryptionKeys.Fingerprint(keys.AesKey)));
            Assert.That(text, Does.Contain("1 previous key"));
            Assert.That(text, Does.Not.Contain(EncryptionKeys.EncodeAesKey(keys.AesKey)));
            Assert.That(new EncryptionKeys().Describe(EncryptionMode.Aes, "none"), Does.Contain("no AES key"));

            EncryptionKeys pair = EncryptionKeys.GenerateRsa(2048);
            Assert.That(pair.Describe(EncryptionMode.Rsa, "x"), Does.Contain("WARNING"), "A client holding a private key is flagged.");
            Assert.That(pair.ForClient(EncryptionMode.Rsa).Describe(EncryptionMode.Rsa, "x"), Does.Not.Contain("WARNING"));
        }

        [Test]
        public void AddPreviousIgnoresDuplicatesTheCurrentKeyAndWrongSizes()
        {
            EncryptionKeys keys = NewAes();
            byte[] old = EncryptionKeys.GenerateAesKey();
            keys.AddPreviousAesKey(old);
            keys.AddPreviousAesKey((byte[])old.Clone());
            keys.AddPreviousAesKey(keys.AesKey);
            keys.AddPreviousAesKey(new byte[5]);
            keys.AddPreviousAesKey(null);
            Assert.That(keys.PreviousAesKeys, Has.Count.EqualTo(1));
        }

        [Test]
        public void PreviousKeysOpenOldFilesButNewFilesUseTheCurrentKey()
        {
            EncryptionKeys oldKeys = NewAes();
            byte[] data = { 1, 2, 3, 4, 5 };
            byte[] writtenWithOld = new EncryptionTransform(EncryptionMode.Aes, oldKeys).Encode(data);

            EncryptionKeys rotated = NewAes();
            rotated.AddPreviousAesKey(oldKeys.AesKey);
            EncryptionTransform transform = new(EncryptionMode.Aes, rotated);
            Assert.That(transform.Decode(writtenWithOld), Is.EqualTo(data));

            byte[] writtenWithNew = transform.Encode(data);
            Assert.That(new EncryptionTransform(EncryptionMode.Aes, rotated).Decode(writtenWithNew), Is.EqualTo(data));
            Assert.Throws<CryptographicException>(() => new EncryptionTransform(EncryptionMode.Aes, oldKeys).Decode(writtenWithNew),
                "New files must not open with only the old key.");
            Assert.Throws<CryptographicException>(() => new EncryptionTransform(EncryptionMode.Aes, NewAes()).Decode(writtenWithOld),
                "Without the history an old file stays closed.");
        }

        [Test]
        public void StoreReadsSavesWrittenBeforeAKeyChange()
        {
            string folder = Path.Combine(Application.temporaryCachePath, "KeyRotation_" + Guid.NewGuid().ToString("N"));
            EncryptionKeys oldKeys = NewAes();
            EncryptionKeys rotated = NewAes();
            rotated.AddPreviousAesKey(oldKeys.AesKey);
            DataStore before = DataStoreFactory.Create(DataRoots.Absolute(folder), DataFormat.Encrypted, oldKeys, keepBackup: false);
            DataStore after = DataStoreFactory.Create(DataRoots.Absolute(folder), DataFormat.Encrypted, rotated, keepBackup: false);
            try
            {
                before.Save("profile", new Holder { value = 7 });
                Assert.That(after.Load<Holder>("profile").Value.value, Is.EqualTo(7));

                after.Save("profile", new Holder { value = 8 });
                Assert.That(before.Load<Holder>("profile").Status, Is.EqualTo(DataStatus.Corrupt));
                Assert.That(after.Load<Holder>("profile").Value.value, Is.EqualTo(8));
            }
            finally
            {
                after.Delete("profile");
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder);
                }
            }
        }

        [Test]
        public void JsonConfigKeepsPreviousKeysAndTheirFingerprint()
        {
            EncryptionKeys keys = NewAes();
            keys.AddPreviousAesKey(EncryptionKeys.GenerateAesKey());
            keys.AddPreviousAesKey(EncryptionKeys.GenerateAesKey());

            EncryptionKeys restored = GameEncryptionKeyConfig.FromJson(GameEncryptionKeyConfig.ToJson(keys, EncryptionMode.Aes));
            Assert.That(restored.AesKey, Is.EqualTo(keys.AesKey));
            Assert.That(restored.PreviousAesKeys, Has.Count.EqualTo(2));
            Assert.That(restored.PreviousAesKeys[1], Is.EqualTo(keys.PreviousAesKeys[1]));
        }

        [Serializable]
        private sealed class Holder
        {
            public int value;
        }
    }
}
