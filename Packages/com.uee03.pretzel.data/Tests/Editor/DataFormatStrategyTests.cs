using System;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace Pretzel.Data.Tests
{
    public sealed class DataFormatStrategyTests
    {
        [Serializable]
        public sealed class Profile
        {
            public string name = "Player";
            public long coins = 1234567890123;
        }

        [Serializable, SchemaVersion(1)]
        public sealed class VersionedProfile
        {
            public string name;
        }

        private string _folder;

        [SetUp]
        public void SetUp() => _folder = Path.Combine(Path.GetTempPath(), "PretzelStrategy_" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folder))
            {
                Directory.Delete(_folder, true);
            }
        }

        [TestCase(DataFormat.PlainText, false)]
        [TestCase(DataFormat.Binary, false)]
        [TestCase(DataFormat.Binary, true)]
        [TestCase(DataFormat.Encrypted, false)]
        [TestCase(DataFormat.Encrypted, true)]
        public void StrategiesSupportStoresPopulateAndBackup(DataFormat format, bool base64)
        {
            EncryptionKeys keys = new() { AesKey = EncryptionKeys.GenerateAesKey() };
            IDataFormatStrategy strategy = DataStoreFactory.CreateStrategy(format, keys);
            DataStore store = DataStoreFactory.CreateWithStrategy(DataRoots.Absolute(_folder), strategy, base64: base64);
            DataKey key = new("profile", slot: 2, folder: "Profiles");
            store.Save(key, new Profile());
            store.Save(key, new Profile { name = "Second" });
            LoadResult<Profile> loaded = store.Load<Profile>(key);
            Assert.That(loaded.Ok, Is.True, loaded.Error);
            Assert.That(loaded.Value.name, Is.EqualTo("Second"));
            Profile target = new();
            Assert.That(store.LoadInto(key, target).Ok, Is.True);
            Assert.That(target.name, Is.EqualTo("Second"));
            Assert.That(store.List("Profiles", 2), Has.Count.EqualTo(1));
            string path = Path.Combine(_folder, "Profiles", "Slot_2", "profile." + strategy.DefaultExtension);
            File.WriteAllBytes(path, new byte[] { 0, 255, 0 });
            LoadResult<Profile> recovered = store.Load<Profile>(key);
            Assert.That(recovered.Ok, Is.True, recovered.Error);
            Assert.That(recovered.RecoveredFromBackup, Is.True);
            Assert.That(recovered.Value.name, Is.EqualTo("Player"));
            Assert.That(recovered.Value.coins, Is.EqualTo(1234567890123));
        }

        [Test]
        public void UnsupportedModeAndFilesAreRejectedWithoutAttemptingAes()
        {
            EncryptionKeys keys = new() { AesKey = EncryptionKeys.GenerateAesKey() };
            Assert.Throws<NotSupportedException>(() => new EncryptedDataStrategy(keys, (EncryptionMode)1));
            EncryptionTransform encryption = new(keys);
            byte[] data = encryption.Encode(Encoding.UTF8.GetBytes("{}"));
            data[5] = 2;
            NotSupportedException error = Assert.Throws<NotSupportedException>(() => encryption.Decode(data));
            Assert.That(error.Message, Does.Contain("Unsupported encryption mode"));
            Directory.CreateDirectory(_folder);
            string path = Path.Combine(_folder, "old.pretzel");
            File.WriteAllBytes(path, data);
            File.WriteAllBytes(path + ".bak", encryption.Encode(Encoding.UTF8.GetBytes("{\"name\":\"Older AES backup\"}")));
            DataStore store = DataStoreFactory.CreateWithStrategy(DataRoots.Absolute(_folder), new EncryptedDataStrategy(keys));
            Assert.That(store.Load<Profile>("old").ErrorCode, Is.EqualTo(DataError.UnsupportedFormat));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(data));
        }

        [Test]
        public void AesVersionOneFixtureRemainsReadableWithCurrentAndHistoricalKeys()
        {
            // Fixed public test key/IV; fixture built independently from the documented v1 layout.
            const string fixture = "UFJUWgEBAAECAwQFBgcICQoLDA0ODzAeJApLZ9dvZ40bNgrz5sGDV/YC7bARs0cW7B0HTU5KI9xvkdRh14M75bpMS5FAKxImJQ0csjRVuCfQrGBP3LssA5cdqpZNB4n+lhS1Xwdd";
            byte[] oldKey = new byte[32];
            for (int i = 0; i < oldKey.Length; i++)
            {
                oldKey[i] = (byte)i;
            }

            EncryptionKeys original = new() { AesKey = oldKey };
            byte[] bytes = Convert.FromBase64String(fixture);
            Assert.That(Encoding.UTF8.GetString(new EncryptionTransform(original).Decode(bytes)),
                Is.EqualTo("{\"name\":\"Previous release\",\"coins\":42}"));
            EncryptionKeys rotated = new() { AesKey = EncryptionKeys.GenerateAesKey() };
            rotated.AddPreviousAesKey(oldKey);
            Assert.That(new EncryptionTransform(rotated).Decode(bytes), Is.EqualTo(new EncryptionTransform(original).Decode(bytes)));
        }

        [Test]
        public void AesRejectsTamperedData()
        {
            EncryptedDataStrategy strategy = new(new EncryptionKeys { AesKey = EncryptionKeys.GenerateAesKey() });
            byte[] data = strategy.Encode(Encoding.UTF8.GetBytes("{\"level\":1}"));
            data[data.Length - 1] ^= 1;
            Assert.Throws<System.Security.Cryptography.CryptographicException>(() => strategy.Decode(data));
        }

        [Test]
        public void InvalidFormatsFailInsteadOfWritingPlainText()
        {
            Assert.Throws<NotSupportedException>(() => DataStoreFactory.CreateStrategy((DataFormat)999));
            Assert.Throws<NotSupportedException>(() => DataFormats.DefaultExtension((DataFormat)999));
            Assert.Throws<NotSupportedException>(() => new EncryptedDataStrategy(new EncryptionKeys(), (EncryptionMode)999));
            Assert.That((int)DataFormat.Binary, Is.EqualTo(1));
            Assert.That((int)DataFormat.Encrypted, Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BinaryPreservesSchemaMigrationsAndBothSerializers(bool jsonUtility)
        {
            ISerializer serializer = jsonUtility ? new JsonUtilitySerializer() : new NewtonsoftSerializer();
            DataStore store = DataStoreFactory.CreateWithStrategy(DataRoots.Absolute(_folder), new BinaryDataStrategy(), serializer);
            store.Save("versioned", new VersionedProfile { name = "Current" });
            LoadResult<VersionedProfile> current = store.Load<VersionedProfile>("versioned");
            Assert.That(current.Ok, Is.True, current.Error);
            Assert.That(current.Value.name, Is.EqualTo("Current"));

            // An unversioned save becomes schema 1 through the registered migration.
            store.Save("old", new Profile());
            DataStoreOptions options = new();
            options.AddTransform(new BinaryDataStrategy());
            options.AddMigration<VersionedProfile>(new DelegateMigration(0, 1, text => text));
            DataStore upgraded = new(DataRoots.Absolute(_folder), serializer, "bin", options);
            LoadResult<VersionedProfile> migrated = upgraded.Load<VersionedProfile>("old");
            Assert.That(migrated.Ok, Is.True, migrated.Error);
            Assert.That(migrated.Value.name, Is.EqualTo("Player"));
        }
    }
}
