using System;
using NUnit.Framework;
using UnityEngine;

namespace Pretzel.Multiplayer.Tests
{
    public sealed class ReconnectStoreTests
    {
        private static DateTime Saved { get; } = new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc);

        private static ReconnectRecord Sample(SessionRole role = SessionRole.Client, SessionMode mode = SessionMode.Online)
        {
            return ReconnectRecord.Create("session-42", role, mode, 1, 2, "Tester", false, Saved);
        }

        [Test]
        public void Record_RoundTripsThroughJson()
        {
            string json = JsonUtility.ToJson(Sample());

            ReconnectRecord loaded = JsonUtility.FromJson<ReconnectRecord>(json);

            Assert.AreEqual("session-42", loaded.SessionId);
            Assert.AreEqual(SessionRole.Client, loaded.Role);
            Assert.AreEqual(SessionMode.Online, loaded.Mode);
            Assert.AreEqual(1, loaded.LocalPlayerIndex);
            Assert.AreEqual(2, loaded.LocalPlayerCount);
            Assert.AreEqual("Tester", loaded.DisplayName);
            Assert.IsFalse(loaded.CrossPlay);
            Assert.AreEqual(Saved, loaded.SavedAtUtc);
        }

        [Test]
        public void Record_ContainsNoSecretsJustIdentifiers()
        {
            string json = JsonUtility.ToJson(Sample());

            StringAssert.DoesNotContain("token", json.ToLowerInvariant());
            StringAssert.DoesNotContain("password", json.ToLowerInvariant());
            StringAssert.DoesNotContain("key", json.ToLowerInvariant());
        }

        [Test]
        public void IsExpired_UsesTheMaximumAge()
        {
            ReconnectRecord record = Sample();

            Assert.IsFalse(record.IsExpired(Saved.AddMinutes(29), TimeSpan.FromMinutes(30)));
            Assert.IsTrue(record.IsExpired(Saved.AddMinutes(31), TimeSpan.FromMinutes(30)));
        }

        [Test]
        public void CanRejoin_OnlyForOnlineClientsWithASessionId()
        {
            Assert.IsTrue(Sample().CanRejoin());
            Assert.IsFalse(Sample(SessionRole.Host).CanRejoin());
            Assert.IsFalse(Sample(SessionRole.Client, SessionMode.Local).CanRejoin());
            Assert.IsFalse(ReconnectRecord.Create(string.Empty, SessionRole.Client, SessionMode.Online, 0, 1, "x", true, Saved).CanRejoin());
        }

        [Test]
        public void InMemoryStore_SavesLoadsAndClears()
        {
            InMemoryReconnectStore store = new InMemoryReconnectStore();
            Assert.IsNull(store.Load());

            ReconnectRecord record = Sample();
            store.Save(record);
            Assert.AreSame(record, store.Load());

            store.Clear();
            Assert.IsNull(store.Load());
        }

        [Test]
        public void PlayerPrefsStore_RoundTripsAndClears()
        {
            string key = "pretzel.multiplayer.tests." + Guid.NewGuid().ToString("N");
            PlayerPrefsReconnectStore store = new PlayerPrefsReconnectStore(key);
            try
            {
                Assert.IsNull(store.Load());

                store.Save(Sample());
                ReconnectRecord loaded = store.Load();

                Assert.IsNotNull(loaded);
                Assert.AreEqual("session-42", loaded.SessionId);
                Assert.AreEqual(Saved, loaded.SavedAtUtc);

                store.Clear();
                Assert.IsNull(store.Load());
            }
            finally
            {
                PlayerPrefs.DeleteKey(key);
            }
        }

        [Test]
        public void PlayerPrefsStore_CorruptJson_ReturnsNullInsteadOfThrowing()
        {
            string key = "pretzel.multiplayer.tests." + Guid.NewGuid().ToString("N");
            try
            {
                PlayerPrefs.SetString(key, "{ this is not json");

                Assert.IsNull(new PlayerPrefsReconnectStore(key).Load());
            }
            finally
            {
                PlayerPrefs.DeleteKey(key);
            }
        }

        [Test]
        public void PlayerPrefsStore_RejectsAnEmptyKey()
        {
            Assert.Throws<ArgumentException>(() => new PlayerPrefsReconnectStore(string.Empty));
        }
    }
}
