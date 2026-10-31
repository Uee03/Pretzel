using NUnit.Framework;
using Pretzel.Data;

namespace Pretzel.Cloud.Tests
{
    public sealed class SyncPlannerTests
    {
        private static readonly DataKey _key = new("profile");

        private sealed class FixedResolver : IConflictResolver
        {
            public ConflictChoice Choice;
            public ConflictInfo Seen;

            public ConflictChoice Resolve(ConflictInfo conflict)
            {
                Seen = conflict;
                return Choice;
            }
        }

        private static SyncEntry Entry(SyncEntryState state, long ticks) => new() { cloudKey = "k", name = "profile", state = state, savedAtUtcTicks = ticks };

        private static SyncAction Plan(SyncEntry entry, bool localExists, bool cloudExists, long cloudTicks, out bool conflict, IConflictResolver resolver = null)
        {
            return SyncPlanner.Plan(entry, localExists, cloudExists, cloudTicks, _key, resolver ?? new NewestWinsConflictResolver(), out conflict);
        }

        [Test]
        public void NothingAnywhereIsInSync()
        {
            Assert.That(Plan(null, false, false, 0, out bool conflict), Is.EqualTo(SyncAction.InSync));
            Assert.That(conflict, Is.False);
        }

        [Test]
        public void LocalOnlyIsPushed()
        {
            Assert.That(Plan(null, true, false, 0, out bool conflict), Is.EqualTo(SyncAction.PushLocal));
            Assert.That(Plan(Entry(SyncEntryState.Synced, 5), true, false, 0, out _), Is.EqualTo(SyncAction.PushLocal));
            Assert.That(conflict, Is.False);
        }

        [Test]
        public void CloudOnlyIsPulled()
        {
            Assert.That(Plan(null, false, true, 10, out bool conflict), Is.EqualTo(SyncAction.PullCloud));
            Assert.That(conflict, Is.False);
        }

        [Test]
        public void SameSaveTimeIsInSyncWhateverTheEntryState()
        {
            Assert.That(Plan(Entry(SyncEntryState.PendingWrite, 7), true, true, 7, out bool conflict), Is.EqualTo(SyncAction.InSync));
            Assert.That(Plan(Entry(SyncEntryState.Synced, 7), true, true, 7, out _), Is.EqualTo(SyncAction.InSync));
            Assert.That(Plan(Entry(SyncEntryState.Failed, 7), true, true, 7, out _), Is.EqualTo(SyncAction.InSync));
            Assert.That(conflict, Is.False);
        }

        [Test]
        public void SyncedLocalFallsBehindWithoutAskingTheResolver()
        {
            FixedResolver resolver = new() { Choice = ConflictChoice.UseLocal };
            Assert.That(Plan(Entry(SyncEntryState.Synced, 5), true, true, 9, out bool conflict, resolver), Is.EqualTo(SyncAction.PullCloud));
            Assert.That(conflict, Is.False);
            Assert.That(resolver.Seen, Is.Null);
        }

        [Test]
        public void CloudNewerStateAlwaysPulls()
        {
            Assert.That(Plan(Entry(SyncEntryState.CloudNewer, 9), true, true, 9, out bool conflict), Is.EqualTo(SyncAction.PullCloud));
            Assert.That(conflict, Is.False);
        }

        [Test]
        public void PendingLocalAgainstDifferentCloudAsksTheResolver()
        {
            FixedResolver resolver = new() { Choice = ConflictChoice.UseCloud };
            Assert.That(Plan(Entry(SyncEntryState.PendingWrite, 5), true, true, 9, out bool conflict, resolver), Is.EqualTo(SyncAction.PullCloud));
            Assert.That(conflict, Is.True);
            Assert.That(resolver.Seen.LocalSavedAtUtcTicks, Is.EqualTo(5));
            Assert.That(resolver.Seen.CloudSavedAtUtcTicks, Is.EqualTo(9));
            Assert.That(resolver.Seen.LocalIsDeletion, Is.False);
            Assert.That(resolver.Seen.Key, Is.EqualTo(_key));

            resolver.Choice = ConflictChoice.UseLocal;
            Assert.That(Plan(Entry(SyncEntryState.PendingWrite, 5), true, true, 9, out _, resolver), Is.EqualTo(SyncAction.PushLocal));
        }

        [Test]
        public void UntrackedLocalDataAgainstCloudDataIsAConflictWithUnknownLocalTime()
        {
            FixedResolver resolver = new() { Choice = ConflictChoice.UseLocal };
            Assert.That(Plan(null, true, true, 9, out bool conflict, resolver), Is.EqualTo(SyncAction.PushLocal));
            Assert.That(conflict, Is.True);
            Assert.That(resolver.Seen.LocalSavedAtUtcTicks, Is.EqualTo(0));
        }

        [Test]
        public void PendingDeleteAgainstExistingCloudDataIsAConflict()
        {
            FixedResolver resolver = new() { Choice = ConflictChoice.UseLocal };
            Assert.That(Plan(Entry(SyncEntryState.PendingDelete, 20), false, true, 9, out bool conflict, resolver), Is.EqualTo(SyncAction.PushLocal));
            Assert.That(conflict, Is.True);
            Assert.That(resolver.Seen.LocalIsDeletion, Is.True);

            resolver.Choice = ConflictChoice.UseCloud;
            Assert.That(Plan(Entry(SyncEntryState.PendingDelete, 20), false, true, 9, out _, resolver), Is.EqualTo(SyncAction.PullCloud));
        }

        [Test]
        public void PendingDeleteWithNoCloudDataIsAlreadyDone()
        {
            Assert.That(Plan(Entry(SyncEntryState.PendingDelete, 20), false, false, 0, out bool conflict), Is.EqualTo(SyncAction.InSync));
            Assert.That(conflict, Is.False);
        }

        [Test]
        public void MissingLocalFileWithNewCloudDataIsPulledEvenWhenTracked()
        {
            Assert.That(Plan(Entry(SyncEntryState.Synced, 5), false, true, 5, out _), Is.EqualTo(SyncAction.PullCloud));
        }
    }
}
