using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Pretzel.Data;
using UnityEngine;
using UnityEngine.TestTools;

namespace Pretzel.Cloud.Tests
{
    public sealed class SyncedDataStoreListTests : SyncedStoreTestBase
    {
        private static readonly DataKey _a = new("a");
        private static readonly DataKey _b = new("b");

        [UnityTest]
        public IEnumerator ListAsyncJoinsDeviceKeysWithCloudOnlyKeys() => Run.Wait(ListAsync());

        private async Awaitable ListAsync()
        {
            SyncedDataStore other = NewStore(NewLocal());
            await other.SaveAsync(_b, new TestProfile("b", 1));
            await other.SaveAsync(new DataKey("c", null, "Other"), new TestProfile("c", 1));

            SyncedDataStore store = NewStore(NewLocal());
            store.Save(_a, new TestProfile("a", 1));

            IReadOnlyList<DataKey> keys = await store.ListAsync();
            Assert.That(keys, Is.EqualTo(new[] { _a, _b }));
            Assert.That(store.List(), Is.EqualTo(new[] { _a }), "the sync List stays local");
            IReadOnlyList<DataKey> otherFolder = await store.ListAsync("Other");
            Assert.That(otherFolder, Is.EqualTo(new[] { new DataKey("c", null, "Other") }));
        }

        [UnityTest]
        public IEnumerator ListAsyncSkipsKeysWithAQueuedDeleteAndFallsBackToLocalOffline() => Run.Wait(ListDeletedAsync());

        private async Awaitable ListDeletedAsync()
        {
            SyncedDataStore store = NewStore(NewLocal());
            await store.SaveAsync(_a, new TestProfile("a", 1));
            await store.SaveAsync(_b, new TestProfile("b", 1));
            Cloud.Faults.Offline = true;
            store.Delete(_a);

            Cloud.Faults.Clear();
            Assert.That(await store.ListAsync(), Is.EqualTo(new[] { _b }), "the cloud still has a, but its delete is queued");

            Cloud.Faults.Offline = true;
            Assert.That(await store.ListAsync(), Is.EqualTo(new[] { _b }));
            Assert.That(store.Status.State, Is.EqualTo(SyncState.Offline));
        }

        [UnityTest]
        public IEnumerator ExistsAsyncAlsoLooksInTheCloud() => Run.Wait(ExistsAsync());

        private async Awaitable ExistsAsync()
        {
            SyncedDataStore other = NewStore(NewLocal());
            await other.SaveAsync(_a, new TestProfile("a", 1));
            SyncedDataStore store = NewStore(NewLocal());

            Assert.That(store.Exists(_a), Is.False);
            Assert.That(await store.ExistsAsync(_a), Is.True);
            Assert.That(await store.ExistsAsync(_b), Is.False);

            Cloud.Faults.Offline = true;
            Assert.That(await store.ExistsAsync(_a), Is.False, "an unreachable cloud counts as absent");
        }

        [UnityTest]
        public IEnumerator SaveSlotsOnTheSyncedStoreBackUpEverySlotAndAFreshDeviceSeesThem() => Run.Wait(SlotsAsync());

        private async Awaitable SlotsAsync()
        {
            SyncedDataStore deviceA = NewStore(NewLocal());
            SaveSlots<TestProfile> slotsA = new(deviceA, "save", "Saves");
            await slotsA.SaveAsync(1, new TestProfile("Ana", 5), new SaveSlotInfo { label = "First" });
            await slotsA.SaveAsync(3, new TestProfile("Ana", 8));
            Assert.That(Cloud.Keys.Count, Is.EqualTo(4), "data and info for two slots");

            SyncedDataStore deviceB = NewStore(NewLocal());
            SaveSlots<TestProfile> slotsB = new(deviceB, "save", "Saves");
            Assert.That(deviceB.ListSlots("Saves"), Is.Empty, "a new device knows nothing until it asks the cloud");
            Assert.That(await deviceB.ListSlotsAsync("Saves"), Is.EqualTo(new[] { 1, 3 }));

            LoadResult<TestProfile> loaded = await slotsB.LoadAsync(3);
            Assert.That(loaded.Value.level, Is.EqualTo(8));
            Assert.That(deviceB.ListSlots("Saves"), Is.EqualTo(new[] { 3 }), "the downloaded slot is now on the device");
        }
    }
}
