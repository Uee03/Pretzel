using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Pretzel.Data;
using UnityEngine;
using UnityEngine.TestTools;

namespace Pretzel.Cloud.Tests
{
    public sealed class SyncedDataStoreSaveTests : SyncedStoreTestBase
    {
        private static readonly DataKey _profile = new("profile");
        private static readonly DataKey _settings = new("settings");
        private static readonly DataKey _inventory = new("inventory");

        private TestProfile CloudProfile(DataKey key) => new NewtonsoftSerializer().Deserialize<TestProfile>(PayloadIn(Cloud, key).data);

        [Test]
        public void SyncSaveWritesLocallyAndQueuesWithoutTouchingTheCloud()
        {
            DataStore local = NewLocal();
            SyncedDataStore store = NewStore(local);

            store.Save(_profile, new TestProfile("Ana", 3));

            Assert.That(local.Load<TestProfile>(_profile).Value.level, Is.EqualTo(3));
            Assert.That(store.Load<TestProfile>(_profile).Value.name, Is.EqualTo("Ana"));
            Assert.That(Cloud.Faults.CallCount, Is.EqualTo(0));
            Assert.That(store.PendingCount, Is.EqualTo(1));
            Assert.That(store.Status.State, Is.EqualTo(SyncState.Pending));
            Assert.That(local.Exists(new DataKey("sync-journal", null, "PretzelCloud")), Is.True, "the queue is saved next to the data");
            Assert.That(local.List().Count, Is.EqualTo(1), "the journal never shows up in the data listing");
        }

        [UnityTest]
        public IEnumerator SaveAsyncUploadsAndMarksTheKeySynced() => Run.Wait(SaveAsyncUploadsAsync());

        private async Awaitable SaveAsyncUploadsAsync()
        {
            SyncedDataStore store = NewStore(NewLocal());
            await store.SaveAsync(_profile, new TestProfile("Ana", 3));

            Assert.That(Cloud.SaveCount, Is.EqualTo(1));
            CloudPayload payload = PayloadIn(Cloud, _profile);
            Assert.That(payload.format, Is.EqualTo(CloudPayload.CurrentFormat));
            Assert.That(payload.savedAtUtcTicks, Is.EqualTo(Clock.Now.Ticks));
            Assert.That(payload.schemaVersion, Is.EqualTo(0));
            Assert.That(CloudProfile(_profile).level, Is.EqualTo(3));
            Assert.That(store.PendingCount, Is.EqualTo(0));
            Assert.That(store.Status.State, Is.EqualTo(SyncState.Synced));
            Assert.That(store.Status.LastSyncedUtc, Is.EqualTo(Clock.Now));
            Assert.That(store.Status.LastError, Is.Null);
        }

        [UnityTest]
        public IEnumerator OfflineSaveAsyncKeepsTheChangeQueuedAndFlushLaterUploadsIt() => Run.Wait(OfflineSaveAsync());

        private async Awaitable OfflineSaveAsync()
        {
            DataStore local = NewLocal();
            SyncedDataStore store = NewStore(local);
            Cloud.Faults.Offline = true;

            await store.SaveAsync(_profile, new TestProfile("Ana", 3));

            Assert.That(local.Load<TestProfile>(_profile).Value.level, Is.EqualTo(3), "the device copy is always written");
            Assert.That(store.PendingCount, Is.EqualTo(1));
            Assert.That(store.Status.State, Is.EqualTo(SyncState.Offline));
            Assert.That(store.Status.LastError.Kind, Is.EqualTo(CloudErrorKind.Offline));
            Assert.That(Cloud.Keys, Is.Empty);

            Cloud.Faults.Offline = false;
            SyncStatus status = await store.FlushAsync();

            Assert.That(status.State, Is.EqualTo(SyncState.Synced));
            Assert.That(store.PendingCount, Is.EqualTo(0));
            Assert.That(CloudProfile(_profile).name, Is.EqualTo("Ana"));
        }

        [UnityTest]
        public IEnumerator SaveTimesGrowEvenWhenTheClockStandsStill() => Run.Wait(StuckClockAsync());

        private async Awaitable StuckClockAsync()
        {
            SyncedDataStore store = NewStore(NewLocal());
            store.Save(_profile, new TestProfile("Ana", 1));
            await store.FlushAsync();
            long first = PayloadIn(Cloud, _profile).savedAtUtcTicks;

            store.Save(_profile, new TestProfile("Ana", 2));
            await store.FlushAsync();
            long second = PayloadIn(Cloud, _profile).savedAtUtcTicks;

            Assert.That(second, Is.GreaterThan(first));
        }

        [UnityTest]
        public IEnumerator OnlyTheLatestSaveOfAKeyIsUploaded() => Run.Wait(CoalesceAsync());

        private async Awaitable CoalesceAsync()
        {
            SyncedDataStore store = NewStore(NewLocal());
            store.Save(_profile, new TestProfile("Ana", 1));
            store.Save(_profile, new TestProfile("Ana", 2));
            store.Save(_profile, new TestProfile("Ana", 3));
            Assert.That(store.PendingCount, Is.EqualTo(1));

            await store.FlushAsync();

            Assert.That(Cloud.SaveCount, Is.EqualTo(1));
            Assert.That(CloudProfile(_profile).level, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator FlushUploadsInTheOrderOfTheChanges() => Run.Wait(OrderAsync());

        private async Awaitable OrderAsync()
        {
            SyncedDataStore store = NewStore(NewLocal());
            List<string> uploaded = new();
            Cloud.SaveHook = key => uploaded.Add(key);

            store.Save(_profile, new TestProfile("Ana", 1));
            store.Save(_settings, new TestProfile("s", 1));
            store.Save(_inventory, new TestProfile("i", 1));
            store.Save(_profile, new TestProfile("Ana", 2));
            await store.FlushAsync();

            Assert.That(uploaded, Is.EqualTo(new[] { CloudKeyOf(_settings), CloudKeyOf(_inventory), CloudKeyOf(_profile) }));
        }

        [UnityTest]
        public IEnumerator FlushStopsAtTheFirstOfflineFailureSoLaterKeysDoNotOvertake() => Run.Wait(StopAtFirstAsync());

        private async Awaitable StopAtFirstAsync()
        {
            SyncedDataStore store = NewStore(NewLocal());
            List<string> uploaded = new();
            Cloud.SaveHook = key => uploaded.Add(key);
            store.Save(_profile, new TestProfile("Ana", 1));
            store.Save(_settings, new TestProfile("s", 1));
            Cloud.Faults.Offline = true;

            SyncStatus offline = await store.FlushAsync();

            Assert.That(offline.State, Is.EqualTo(SyncState.Offline));
            Assert.That(offline.PendingCount, Is.EqualTo(2));
            Assert.That(uploaded, Is.EqualTo(new[] { CloudKeyOf(_profile), CloudKeyOf(_profile), CloudKeyOf(_profile) }), "three attempts at the first key, none at the second");

            uploaded.Clear();
            Cloud.Faults.Offline = false;
            await store.FlushAsync();
            Assert.That(uploaded, Is.EqualTo(new[] { CloudKeyOf(_profile), CloudKeyOf(_settings) }));
            Assert.That(store.PendingCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator TransientFailuresAreRetriedWithBackoffInsideAFlush() => Run.Wait(BackoffAsync());

        private async Awaitable BackoffAsync()
        {
            SyncedDataStore store = NewStore(NewLocal());
            store.Save(_profile, new TestProfile("Ana", 1));
            Cloud.Faults.Enqueue(CloudErrorKind.ServiceUnavailable, 2);

            SyncStatus status = await store.FlushAsync();

            Assert.That(status.State, Is.EqualTo(SyncState.Synced));
            Assert.That(Cloud.SaveCount, Is.EqualTo(3));
            Assert.That(Delay.Delays, Is.EqualTo(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2) }));
        }

        [UnityTest]
        public IEnumerator AServiceRejectionParksTheKeyAndTheQueueContinues() => Run.Wait(ParkAsync());

        private async Awaitable ParkAsync()
        {
            SyncedDataStore store = NewStore(NewLocal());
            store.Save(_profile, new TestProfile("Ana", 1));
            store.Save(_settings, new TestProfile("s", 1));
            Cloud.Faults.Enqueue(CloudErrorKind.InvalidRequest, 1);

            SyncStatus status = await store.FlushAsync();

            Assert.That(status.FailedCount, Is.EqualTo(1));
            Assert.That(status.PendingCount, Is.EqualTo(0));
            Assert.That(status.State, Is.EqualTo(SyncState.Error));
            Assert.That(Cloud.TryGetValue(CloudKeyOf(_settings), out _), Is.True, "the key behind the rejected one still uploaded");
            Assert.That(Cloud.TryGetValue(CloudKeyOf(_profile), out _), Is.False);

            store.Save(_profile, new TestProfile("Ana", 2));
            Assert.That(store.Status.FailedCount, Is.EqualTo(0), "saving the key again retries it");
            await store.FlushAsync();
            Assert.That(CloudProfile(_profile).level, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator NotSignedInStopsTheQueueAndIsReported() => Run.Wait(NotSignedInAsync());

        private async Awaitable NotSignedInAsync()
        {
            SyncedDataStore store = NewStore(NewLocal());
            store.Save(_profile, new TestProfile("Ana", 1));
            Cloud.Faults.Enqueue(CloudErrorKind.NotSignedIn, 5);

            SyncStatus status = await store.FlushAsync();

            Assert.That(status.State, Is.EqualTo(SyncState.NotSignedIn));
            Assert.That(status.PendingCount, Is.EqualTo(1));
            Assert.That(Cloud.SaveCount, Is.EqualTo(1), "a missing sign-in is not retried");

            Cloud.Faults.Clear();
            Assert.That((await store.FlushAsync()).State, Is.EqualTo(SyncState.Synced));
        }

        [UnityTest]
        public IEnumerator WithUploadOnSaveOffSaveAsyncOnlyQueues() => Run.Wait(QueueOnlyAsync());

        private async Awaitable QueueOnlyAsync()
        {
            SyncedDataStore store = NewStore(NewLocal(), options => options.UploadOnSave = false);
            await store.SaveAsync(_profile, new TestProfile("Ana", 1));
            Assert.That(Cloud.SaveCount, Is.EqualTo(0));
            Assert.That(store.PendingCount, Is.EqualTo(1));
            await store.FlushAsync();
            Assert.That(Cloud.SaveCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ACancelledFlushThrowsAndTheQueueSurvives() => Run.Wait(CancelAsync());

        private async Awaitable CancelAsync()
        {
            SyncedDataStore store = NewStore(NewLocal());
            store.Save(_profile, new TestProfile("Ana", 1));

            using (CancellationTokenSource before = new())
            {
                before.Cancel();
                bool threw = false;
                try
                {
                    await store.FlushAsync(before.Token);
                }
                catch (OperationCanceledException)
                {
                    threw = true;
                }

                Assert.That(threw, Is.True);
            }

            using (CancellationTokenSource during = new())
            {
                Cloud.Faults.Offline = true;
                Delay.OnDelay = during.Cancel;
                bool threw = false;
                try
                {
                    await store.FlushAsync(during.Token);
                }
                catch (OperationCanceledException)
                {
                    threw = true;
                }

                Assert.That(threw, Is.True, "cancelling during a backoff wait stops the flush");
            }

            Assert.That(store.PendingCount, Is.EqualTo(1));
            Assert.That(store.Status.State, Is.Not.EqualTo(SyncState.Syncing), "a cancelled flush leaves the Syncing state");

            Cloud.Faults.Clear();
            Delay.OnDelay = null;
            Assert.That((await store.FlushAsync()).State, Is.EqualTo(SyncState.Synced), "the store still works after a cancelled flush");
        }

        [UnityTest]
        public IEnumerator SyncFilterKeepsKeysOnTheDevice() => Run.Wait(FilterAsync());

        private async Awaitable FilterAsync()
        {
            DataStore local = NewLocal();
            SyncedDataStore store = NewStore(local, options => options.SyncFilter = key => !string.Equals(key.Name, "thumbnail", StringComparison.Ordinal));
            DataKey thumbnail = new("thumbnail");

            await store.SaveAsync(thumbnail, new TestProfile("t", 1));
            store.Save(_profile, new TestProfile("Ana", 1));

            Assert.That(local.Exists(thumbnail), Is.True);
            Assert.That(store.PendingCount, Is.EqualTo(1));
            await store.FlushAsync();
            Assert.That(Cloud.Keys, Is.EqualTo(new[] { CloudKeyOf(_profile) }));
        }

        [Test]
        public void AKeyTooLongForTheCloudFailsBeforeAnythingIsWritten()
        {
            DataStore local = NewLocal();
            SyncedDataStore store = NewStore(local);
            DataKey tooLong = new(new string('é', 100));
            Assert.Throws<ArgumentException>(() => store.Save(tooLong, new TestProfile("x", 1)));
            Assert.That(local.Exists(tooLong), Is.False);
            Assert.That(store.PendingCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator TheQueueSurvivesARestart() => Run.Wait(RestartAsync());

        private async Awaitable RestartAsync()
        {
            string folder = NewFolder();
            SyncedDataStore first = NewStore(NewLocal(folder));
            Cloud.Faults.Offline = true;
            first.Save(_profile, new TestProfile("Ana", 7));
            first.Delete(_settings);
            await first.FlushAsync();
            Assert.That(first.PendingCount, Is.EqualTo(2));
            Cloud.Seed(CloudKeyOf(_settings), "{}");
            Cloud.Faults.Clear();

            SyncedDataStore second = NewStore(NewLocal(folder));
            Assert.That(second.PendingCount, Is.EqualTo(2), "the queue was read back from the local store");
            await second.FlushAsync();

            Assert.That(CloudProfile(_profile).level, Is.EqualTo(7));
            Assert.That(Cloud.TryGetValue(CloudKeyOf(_settings), out _), Is.False, "the queued delete ran too");
            Assert.That(second.PendingCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator StatusChangesAreRaisedOnTheStore() => Run.Wait(StatusEventsAsync());

        private async Awaitable StatusEventsAsync()
        {
            SyncedDataStore store = NewStore(NewLocal());
            List<SyncState> states = new();
            store.StatusChanged += status => states.Add(status.State);

            await store.SaveAsync(_profile, new TestProfile("Ana", 1));

            Assert.That(states, Does.Contain(SyncState.Pending));
            Assert.That(states, Does.Contain(SyncState.Syncing));
            Assert.That(states[states.Count - 1], Is.EqualTo(SyncState.Synced));
        }

        [UnityTest]
        public IEnumerator DeleteQueuesAndTheFlushDeletesFromTheCloud() => Run.Wait(DeleteAsync());

        private async Awaitable DeleteAsync()
        {
            DataStore local = NewLocal();
            SyncedDataStore store = NewStore(local);
            await store.SaveAsync(_profile, new TestProfile("Ana", 1));
            Assert.That(Cloud.Keys.Count, Is.EqualTo(1));

            Cloud.Faults.Offline = true;
            bool existed = await store.DeleteAsync(_profile);

            Assert.That(existed, Is.True);
            Assert.That(local.Exists(_profile), Is.False);
            Assert.That(store.PendingCount, Is.EqualTo(1));
            Assert.That(Cloud.Keys.Count, Is.EqualTo(1));

            Cloud.Faults.Clear();
            await store.FlushAsync();
            Assert.That(Cloud.Keys, Is.Empty);
            Assert.That(store.PendingCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator SavingAfterADeleteTurnsTheQueuedDeleteIntoAWrite() => Run.Wait(DeleteThenSaveAsync());

        private async Awaitable DeleteThenSaveAsync()
        {
            SyncedDataStore store = NewStore(NewLocal());
            await store.SaveAsync(_profile, new TestProfile("Ana", 1));
            Cloud.Faults.Offline = true;
            store.Delete(_profile);
            store.Save(_profile, new TestProfile("Ana", 2));
            Assert.That(store.PendingCount, Is.EqualTo(1));

            Cloud.Faults.Clear();
            await store.FlushAsync();

            Assert.That(Cloud.DeleteCount, Is.EqualTo(0), "no delete is sent when the key was saved again");
            Assert.That(CloudProfile(_profile).level, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator ASaveDuringAnUploadStaysQueuedOnTopOfTheNewCloudState() => Run.Wait(SaveDuringUploadAsync());

        private async Awaitable SaveDuringUploadAsync()
        {
            SyncedDataStore store = NewStore(NewLocal());
            bool saved = false;
            Cloud.SaveHook = key =>
            {
                if (!saved)
                {
                    saved = true;
                    store.Save(_profile, new TestProfile("Ana", 2));
                }
            };
            store.Save(_profile, new TestProfile("Ana", 1));

            await store.FlushAsync();
            Assert.That(store.PendingCount, Is.EqualTo(1), "the newer save is not marked as uploaded");

            await store.FlushAsync();
            Assert.That(store.PendingCount, Is.EqualTo(0));
            Assert.That(CloudProfile(_profile).level, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator TheSyncLoopFlushesUntilCancelled() => Run.Wait(LoopAsync());

        private async Awaitable LoopAsync()
        {
            SyncedDataStore store = NewStore(NewLocal());
            store.Save(_profile, new TestProfile("Ana", 1));
            using CancellationTokenSource source = new();
            int waits = 0;
            Delay.OnDelay = () =>
            {
                waits++;
                if (waits == 1)
                {
                    store.Save(_settings, new TestProfile("s", 1));
                }
                else
                {
                    source.Cancel();
                }
            };

            bool cancelled = false;
            try
            {
                await store.RunSyncLoopAsync(TimeSpan.FromSeconds(30), source.Token);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }

            Assert.That(cancelled, Is.True);
            Assert.That(Cloud.Keys.Count, Is.EqualTo(2), "each pass uploaded what was queued before it");
            Assert.That(Delay.Delays, Is.EqualTo(new[] { TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30) }));
            Assert.Throws<ArgumentOutOfRangeException>(() => store.RunSyncLoopAsync(TimeSpan.Zero, CancellationToken.None));
        }
    }
}
