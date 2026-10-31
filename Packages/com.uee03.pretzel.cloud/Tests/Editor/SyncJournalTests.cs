using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pretzel.Data;

namespace Pretzel.Cloud.Tests
{
    public sealed class SyncJournalTests
    {
        private static readonly DataKey _a = new("a", 1, "Saves");
        private static readonly DataKey _b = new("b");
        private static readonly DataKey _c = new("c");

        [Test]
        public void PendingEntriesComeBackInTheOrderOfTheLatestChange()
        {
            SyncJournal journal = new();
            journal.RecordWrite(_a, "ka", 1, "pa");
            journal.RecordWrite(_b, "kb", 2, "pb");
            journal.RecordDelete(_c, "kc", 3);
            journal.RecordWrite(_a, "ka", 4, "pa2");

            IReadOnlyList<SyncEntry> pending = journal.PendingInOrder();
            Assert.That(pending.Select(e => e.cloudKey).ToArray(), Is.EqualTo(new[] { "kb", "kc", "ka" }));
            Assert.That(journal.CountPending(), Is.EqualTo(3));
            Assert.That(journal.Find("ka").payload, Is.EqualTo("pa2"));
            Assert.That(journal.entries.Count, Is.EqualTo(3), "a second change of the same key updates its entry");
        }

        [Test]
        public void SyncingClearsThePayloadAndRemovesFromThePendingList()
        {
            SyncJournal journal = new();
            SyncEntry entry = journal.RecordWrite(_b, "kb", 5, "payload");
            journal.MarkSynced(entry, 5, "lock-1");
            Assert.That(entry.state, Is.EqualTo(SyncEntryState.Synced));
            Assert.That(entry.payload, Is.Empty);
            Assert.That(entry.writeLock, Is.EqualTo("lock-1"));
            Assert.That(journal.CountPending(), Is.EqualTo(0));
        }

        [Test]
        public void FailedEntriesAreParkedNotPending()
        {
            SyncJournal journal = new();
            SyncEntry entry = journal.RecordWrite(_b, "kb", 5, "payload");
            journal.MarkFailed(entry, new CloudError(CloudErrorKind.InvalidRequest, "too big"));
            Assert.That(journal.CountPending(), Is.EqualTo(0));
            Assert.That(journal.CountFailed(), Is.EqualTo(1));
            Assert.That(entry.errorKind, Is.EqualTo((int)CloudErrorKind.InvalidRequest));
            Assert.That(entry.errorMessage, Is.EqualTo("too big"));

            journal.RecordWrite(_b, "kb", 6, "again");
            Assert.That(journal.CountFailed(), Is.EqualTo(0), "saving the key again retries it");
            Assert.That(entry.errorMessage, Is.Empty);
        }

        [Test]
        public void EntriesRebuildTheirDataKey()
        {
            SyncJournal journal = new();
            SyncEntry slotted = journal.RecordWrite(_a, "ka", 1, "p");
            SyncEntry plain = journal.RecordWrite(_b, "kb", 1, "p");
            Assert.That(slotted.ToDataKey(), Is.EqualTo(_a));
            Assert.That(plain.ToDataKey(), Is.EqualTo(_b));
        }

        [Test]
        public void RemoveForgetsTheEntry()
        {
            SyncJournal journal = new();
            SyncEntry entry = journal.RecordDelete(_b, "kb", 1);
            journal.Remove(entry);
            Assert.That(journal.Find("kb"), Is.Null);
            Assert.That(journal.entries, Is.Empty);
        }

        [Test]
        public void TrackSyncedCreatesAnEntryForAnUntrackedKey()
        {
            SyncJournal journal = new();
            SyncEntry entry = journal.TrackSynced(_c, "kc", 9, "lock");
            Assert.That(entry.state, Is.EqualTo(SyncEntryState.Synced));
            Assert.That(entry.savedAtUtcTicks, Is.EqualTo(9));
            Assert.That(journal.Find("kc"), Is.SameAs(entry));
        }

        [TestCase(typeof(NewtonsoftSerializer))]
        [TestCase(typeof(JsonUtilitySerializer))]
        public void SurvivesSerializationWithEitherSerializer(Type serializerType)
        {
            ISerializer serializer = (ISerializer)Activator.CreateInstance(serializerType);
            SyncJournal journal = new();
            journal.RecordWrite(_a, "ka", 11, "payload-a");
            SyncEntry deleted = journal.RecordDelete(_b, "kb", 12);
            deleted.writeLock = "lock-b";

            SyncJournal restored = serializer.Deserialize<SyncJournal>(serializer.Serialize(journal, false));
            Assert.That(restored.entries.Count, Is.EqualTo(2));
            Assert.That(restored.nextSequence, Is.EqualTo(journal.nextSequence));
            SyncEntry a = restored.Find("ka");
            Assert.That(a.state, Is.EqualTo(SyncEntryState.PendingWrite));
            Assert.That(a.payload, Is.EqualTo("payload-a"));
            Assert.That(a.ToDataKey(), Is.EqualTo(_a));
            SyncEntry b = restored.Find("kb");
            Assert.That(b.state, Is.EqualTo(SyncEntryState.PendingDelete));
            Assert.That(b.writeLock, Is.EqualTo("lock-b"));
            Assert.That(restored.PendingInOrder().Select(e => e.cloudKey).ToArray(), Is.EqualTo(new[] { "ka", "kb" }));
        }

        [Test]
        public void TheJournalFileDoesNotContainComputedProperties()
        {
            SyncJournal journal = new();
            journal.RecordWrite(_b, "kb", 1, "p");
            string json = new NewtonsoftSerializer().Serialize(journal, false);
            Assert.That(json, Does.Not.Contain("IsPending"));
        }
    }
}
