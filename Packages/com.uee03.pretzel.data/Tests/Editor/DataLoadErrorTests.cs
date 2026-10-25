using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Pretzel.Data.Tests
{
    public sealed class DataLoadErrorTests
    {
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "PretzelErrors_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_folder, true);

        [Serializable, SchemaVersion(2)]
        private sealed class Profile
        {
            public int level;
        }

        [Test]
        public void InvalidJsonAndMissingDataAreDifferentResults()
        {
            DataStore store = new(DataRoots.Absolute(_folder));
            Assert.That(store.Load<Profile>("profile").Status, Is.EqualTo(DataStatus.NotFound));
            File.WriteAllText(Path.Combine(_folder, "profile.json"), "{");
            LoadResult<Profile> corrupt = store.Load<Profile>("profile");
            Assert.That(corrupt.Status, Is.EqualTo(DataStatus.Corrupt));
            Assert.That(corrupt.ErrorCode, Is.EqualTo(DataError.InvalidData));
            Assert.That(corrupt.Error, Is.Not.Empty);
        }

        [Test]
        public void WrongKeyAndMissingKeyHaveActionableCodes()
        {
            EncryptionKeys keys = new() { AesKey = EncryptionKeys.GenerateAesKey() };
            DataStore writer = DataStoreFactory.Create(DataRoots.Absolute(_folder), DataFormat.Encrypted, keys);
            writer.Save("profile", new Profile { level = 4 });
            DataStore wrongKey = DataStoreFactory.Create(DataRoots.Absolute(_folder), DataFormat.Encrypted,
                new EncryptionKeys { AesKey = EncryptionKeys.GenerateAesKey() });
            Assert.That(wrongKey.Load<Profile>("profile").ErrorCode, Is.EqualTo(DataError.AuthenticationFailed));

            keys.AesKey = null;
            Assert.That(writer.Load<Profile>("profile").Ok, Is.True, "The store keeps its own copy of the keys.");

            // A raw transform shares the caller's keys, so removing the key afterwards is reported as MissingKey.
            EncryptionKeys shared = new() { AesKey = EncryptionKeys.GenerateAesKey() };
            DataStoreOptions options = new();
            options.Transforms.Add(new EncryptionTransform(shared));
            DataStore revoked = new(DataRoots.Absolute(_folder), extension: "pretzel", options: options);
            shared.AesKey = null;
            Assert.That(revoked.Load<Profile>("profile").ErrorCode, Is.EqualTo(DataError.MissingKey));
            string path = Path.Combine(_folder, "profile.pretzel");
            byte[] original = File.ReadAllBytes(path);
            Assert.That(() => revoked.Save("profile", new Profile { level = 5 }),
                Throws.InstanceOf<InvalidOperationException>());
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
        }

        [Test]
        public void FileReadFailureIsNotReportedAsBadJson()
        {
            string path = Path.Combine(_folder, "profile.json");
            File.WriteAllText(path, "{}");
            using FileStream locked = new(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            DataStore store = new(DataRoots.Absolute(_folder));
            Assert.That(store.Load<Profile>("profile").ErrorCode, Is.EqualTo(DataError.IoFailure));
        }

        [Test]
        public void ConfigCallbackRegistersMigrationsBeforeStoreConstruction()
        {
            File.WriteAllText(Path.Combine(_folder, "profile.json"), "{\"schemaVersion\":1,\"data\":{\"level\":3}}");
            DataIOConfig config = ScriptableObject.CreateInstance<DataIOConfig>();
            try
            {
                DataStore oldSetup = config.CreateStore(DataRoots.Absolute(_folder));
                Assert.That(oldSetup.Load<Profile>("profile").ErrorCode, Is.EqualTo(DataError.SchemaMismatch));
                DataStore upgraded = config.CreateStore(DataRoots.Absolute(_folder), configure: options =>
                    options.AddMigration<Profile>(new DelegateMigration(1, 2, json => json)));
                LoadResult<Profile> loaded = upgraded.Load<Profile>("profile");
                Assert.That(loaded.Ok, Is.True, loaded.Error);
                Assert.That(loaded.Value.level, Is.EqualTo(3));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void UnsupportedSerializedModeFailsBeforeResolvingKeys()
        {
            DataIOConfig config = ScriptableObject.CreateInstance<DataIOConfig>();
            try
            {
                config.format = DataFormat.Encrypted;
                config.encryptionMode = (EncryptionMode)1;
                Assert.Throws<NotSupportedException>(() => config.CreateStore(DataRoots.Absolute(_folder)));
                Assert.That((int)EncryptionMode.Aes, Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}
