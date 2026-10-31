using System;
using System.Collections;
using System.Threading;
using NUnit.Framework;
using Pretzel.Data;
using UnityEngine;
using UnityEngine.TestTools;

namespace Pretzel.Cloud.Tests
{
    public sealed class SyncedDataStoreLoadTests : SyncedStoreTestBase
    {
        private static readonly DataKey _profile = new("profile");

        private static readonly TimeSpan _step = TimeSpan.FromMinutes(1);

        [UnityTest]
        public IEnumerator ANewDeviceDownloadsTheCloudCopy() => Run.Wait(NewDeviceAsync());

        private async Awaitable NewDeviceAsync()
        {
            SyncedDataStore deviceA = NewStore(NewLocal());
            await deviceA.SaveAsync(_profile, new TestProfile("Ana", 5));

            DataStore localB = NewLocal();
            SyncedDataStore deviceB = NewStore(localB);
            LoadResult<TestProfile> loaded = await deviceB.LoadAsync<TestProfile>(_profile);

            Assert.That(loaded.Ok, Is.True);
            Assert.That(loaded.Value.level, Is.EqualTo(5));
            Assert.That(localB.Load<TestProfile>(_profile).Value.name, Is.EqualTo("Ana"), "the download was written to the device");
            Assert.That(deviceB.PendingCount, Is.EqualTo(0));
            Assert.That(deviceB.Status.State, Is.EqualTo(SyncState.Synced));
            Assert.That(deviceB.Status.ResolvedConflicts, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator MissingEverywhereIsNotFound() => Run.Wait(MissingAsync());

        private async Awaitable MissingAsync()
        {
            SyncedDataStore store = NewStore(NewLocal());
            LoadResult<TestProfile> loaded = await store.LoadAsync<TestProfile>(_profile);
            Assert.That(loaded.Status, Is.EqualTo(DataStatus.NotFound));
            Assert.That(store.Status.State, Is.EqualTo(SyncState.Synced));
        }

        [UnityTest]
        public IEnumerator OfflineLoadReturnsTheDeviceCopy() => Run.Wait(OfflineLoadAsync());

        private async Awaitable OfflineLoadAsync()
        {
            SyncedDataStore store = NewStore(NewLocal());
            store.Save(_profile, new TestProfile("Ana", 2));
            Cloud.Faults.Offline = true;

            LoadResult<TestProfile> loaded = await store.LoadAsync<TestProfile>(_profile);

            Assert.That(loaded.Value.level, Is.EqualTo(2));
            Assert.That(store.Status.State, Is.EqualTo(SyncState.Offline));
            Assert.That(store.PendingCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ASyncedDeviceFastForwardsToANewerCloudCopyWithoutAConflict() => Run.Wait(FastForwardAsync());

        private async Awaitable FastForwardAsync()
        {
            SyncedDataStore deviceA = NewStore(NewLocal());
            SyncedDataStore deviceB = NewStore(NewLocal());
            await deviceA.SaveAsync(_profile, new TestProfile("Ana", 1));
            await deviceB.LoadAsync<TestProfile>(_profile);

            Clock.Advance(_step);
            await deviceA.SaveAsync(_profile, new TestProfile("Ana", 5));
            LoadResult<TestProfile> loaded = await deviceB.LoadAsync<TestProfile>(_profile);

            Assert.That(loaded.Value.level, Is.EqualTo(5));
            Assert.That(deviceB.Status.ResolvedConflicts, Is.EqualTo(0));
            Assert.That(deviceB.Load<TestProfile>(_profile).Value.level, Is.EqualTo(5));
        }

        [UnityTest]
        public IEnumerator NewestWinsKeepsALaterDeviceChangeAndUploadsIt() => Run.Wait(LocalNewerAsync());

        private async Awaitable LocalNewerAsync()
        {
            SyncedDataStore deviceA = NewStore(NewLocal());
            SyncedDataStore deviceB = NewStore(NewLocal());
            await deviceA.SaveAsync(_profile, new TestProfile("Ana", 1));
            await deviceB.LoadAsync<TestProfile>(_profile);

            Clock.Advance(_step);
            await deviceA.SaveAsync(_profile, new TestProfile("Ana", 2));
            Clock.Advance(_step);
            deviceB.Save(_profile, new TestProfile("Ana", 3));

            LoadResult<TestProfile> loaded = await deviceB.LoadAsync<TestProfile>(_profile);

            Assert.That(loaded.Value.level, Is.EqualTo(3));
            Assert.That(deviceB.Status.ResolvedConflicts, Is.EqualTo(1));
            Assert.That(deviceB.PendingCount, Is.EqualTo(0), "the winning device copy was uploaded");
            Assert.That(new NewtonsoftSerializer().Deserialize<TestProfile>(PayloadIn(Cloud, _profile).data).level, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator NewestWinsTakesALaterCloudCopyOverDeviceChanges() => Run.Wait(CloudNewerAsync());

        private async Awaitable CloudNewerAsync()
        {
            SyncedDataStore deviceA = NewStore(NewLocal());
            SyncedDataStore deviceB = NewStore(NewLocal());
            await deviceA.SaveAsync(_profile, new TestProfile("Ana", 1));
            await deviceB.LoadAsync<TestProfile>(_profile);

            Clock.Advance(_step);
            deviceB.Save(_profile, new TestProfile("Ana", 3));
            Clock.Advance(_step);
            await deviceA.SaveAsync(_profile, new TestProfile("Ana", 2));

            LoadResult<TestProfile> loaded = await deviceB.LoadAsync<TestProfile>(_profile);

            Assert.That(loaded.Value.level, Is.EqualTo(2));
            Assert.That(deviceB.Status.ResolvedConflicts, Is.EqualTo(1));
            Assert.That(deviceB.PendingCount, Is.EqualTo(0), "the discarded device change is no longer queued");
            Assert.That(deviceB.Load<TestProfile>(_profile).Value.level, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator KeepLocalWinsEvenWhenTheCloudCopyIsNewerAndEndsUpNewer() => Run.Wait(KeepLocalAsync());

        private async Awaitable KeepLocalAsync()
        {
            SyncedDataStore deviceA = NewStore(NewLocal());
            SyncedDataStore deviceB = NewStore(NewLocal(), options => options.ConflictResolver = new KeepLocalConflictResolver());
            await deviceA.SaveAsync(_profile, new TestProfile("Ana", 1));
            await deviceB.LoadAsync<TestProfile>(_profile);
            deviceB.Save(_profile, new TestProfile("Ana", 3));
            Clock.Advance(_step);
            await deviceA.SaveAsync(_profile, new TestProfile("Ana", 2));
            long cloudTicks = PayloadIn(Cloud, _profile).savedAtUtcTicks;

            LoadResult<TestProfile> loaded = await deviceB.LoadAsync<TestProfile>(_profile);

            Assert.That(loaded.Value.level, Is.EqualTo(3));
            CloudPayload after = PayloadIn(Cloud, _profile);
            Assert.That(new NewtonsoftSerializer().Deserialize<TestProfile>(after.data).level, Is.EqualTo(3));
            Assert.That(after.savedAtUtcTicks, Is.GreaterThan(cloudTicks), "a winning device copy must not look older than the copy it replaced");
        }

        [UnityTest]
        public IEnumerator KeepCloudDiscardsALaterDeviceChange() => Run.Wait(KeepCloudAsync());

        private async Awaitable KeepCloudAsync()
        {
            SyncedDataStore deviceA = NewStore(NewLocal());
            SyncedDataStore deviceB = NewStore(NewLocal(), options => options.ConflictResolver = new KeepCloudConflictResolver());
            await deviceA.SaveAsync(_profile, new TestProfile("Ana", 1));
            await deviceB.LoadAsync<TestProfile>(_profile);
            Clock.Advance(_step);
            await deviceA.SaveAsync(_profile, new TestProfile("Ana", 2));
            Clock.Advance(_step);
            deviceB.Save(_profile, new TestProfile("Ana", 3));

            LoadResult<TestProfile> loaded = await deviceB.LoadAsync<TestProfile>(_profile);

            Assert.That(loaded.Value.level, Is.EqualTo(2));
            Assert.That(deviceB.PendingCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator ExistingDeviceDataIsUploadedWhenTheCloudHasNone() => Run.Wait(AdoptLocalAsync());

        private async Awaitable AdoptLocalAsync()
        {
            DataStore local = NewLocal();
            local.Save(_profile, new TestProfile("Old", 9));
            SyncedDataStore store = NewStore(local);

            LoadResult<TestProfile> loaded = await store.LoadAsync<TestProfile>(_profile);

            Assert.That(loaded.Value.level, Is.EqualTo(9));
            Assert.That(Cloud.TryGetValue(CloudKeyOf(_profile), out _), Is.True, "data saved before syncing existed is backed up");
            Assert.That(store.PendingCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator UntrackedDeviceDataAgainstCloudDataGoesToTheResolver() => Run.Wait(UntrackedConflictAsync());

        private async Awaitable UntrackedConflictAsync()
        {
            SyncedDataStore other = NewStore(NewLocal());
            await other.SaveAsync(_profile, new TestProfile("Cloud", 1));

            DataStore local = NewLocal();
            local.Save(_profile, new TestProfile("Old", 9));
            SyncedDataStore keepLocal = NewStore(local, options => options.ConflictResolver = new KeepLocalConflictResolver());

            LoadResult<TestProfile> loaded = await keepLocal.LoadAsync<TestProfile>(_profile);

            Assert.That(loaded.Value.name, Is.EqualTo("Old"));
            Assert.That(keepLocal.Status.ResolvedConflicts, Is.EqualTo(1));
            Assert.That(new NewtonsoftSerializer().Deserialize<TestProfile>(PayloadIn(Cloud, _profile).data).name, Is.EqualTo("Old"));
        }

        [UnityTest]
        public IEnumerator AnUnreadableCloudCopyNeverReplacesTheDeviceCopy() => Run.Wait(UnreadableAsync());

        private async Awaitable UnreadableAsync()
        {
            SyncedDataStore store = NewStore(NewLocal());
            store.Save(_profile, new TestProfile("Ana", 2));
            Cloud.Seed(CloudKeyOf(_profile), "this is not a payload");

            LoadResult<TestProfile> loaded = await store.LoadAsync<TestProfile>(_profile);

            Assert.That(loaded.Value.level, Is.EqualTo(2));
            Assert.That(store.Status.LastError.Kind, Is.EqualTo(CloudErrorKind.Serialization));
            Assert.That(store.Status.State, Is.EqualTo(SyncState.Error));
            Assert.That(store.PendingCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ASaveMadeWhileTheCloudLoadIsRunningWins() => Run.Wait(SaveDuringLoadAsync());

        private async Awaitable SaveDuringLoadAsync()
        {
            SyncedDataStore deviceA = NewStore(NewLocal());
            SyncedDataStore deviceB = NewStore(NewLocal());
            await deviceA.SaveAsync(_profile, new TestProfile("Ana", 1));
            await deviceB.LoadAsync<TestProfile>(_profile);
            Clock.Advance(_step);
            await deviceA.SaveAsync(_profile, new TestProfile("Ana", 2));

            Cloud.LoadHook = key => deviceB.Save(_profile, new TestProfile("Ana", 99));
            LoadResult<TestProfile> loaded = await deviceB.LoadAsync<TestProfile>(_profile);

            Assert.That(loaded.Value.level, Is.EqualTo(99), "the newer device save is not overwritten by the older download");
            Assert.That(deviceB.PendingCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ALaterCloudSchemaIsNotAppliedAndOlderOnesAreMigrated() => Run.Wait(SchemaAsync());

        private async Awaitable SchemaAsync()
        {
            DataKey key = new("versioned");
            NewtonsoftSerializer serializer = new();
            DataStoreOptions localOptions = new();
            DelegateMigration migration = new(1, 2, text => text.Replace("\"name\"", "\"displayName\""));
            localOptions.AddMigration<TestVersionedProfile>(migration);
            DataStore local = new(DataRoots.Absolute(NewFolder()), serializer, "json", localOptions);
            SyncedDataStore store = NewStore(local, options => options.AddMigration<TestVersionedProfile>(migration));

            CloudPayload older = new() { savedAtUtcTicks = Clock.Now.Ticks, schemaVersion = 1, data = "{\"name\":\"Ana\",\"level\":4}" };
            Cloud.Seed(CloudKeyOf(key), serializer.Serialize(older, false));
            LoadResult<TestVersionedProfile> migrated = await store.LoadAsync<TestVersionedProfile>(key);
            Assert.That(migrated.Ok, Is.True);
            Assert.That(migrated.Value.displayName, Is.EqualTo("Ana"));
            Assert.That(migrated.Value.level, Is.EqualTo(4));

            Clock.Advance(_step);
            CloudPayload newer = new() { savedAtUtcTicks = Clock.Now.Ticks, schemaVersion = 3, data = "{\"displayName\":\"Future\",\"level\":9}" };
            Cloud.Seed(CloudKeyOf(key), serializer.Serialize(newer, false));
            LoadResult<TestVersionedProfile> kept = await store.LoadAsync<TestVersionedProfile>(key);
            Assert.That(kept.Value.displayName, Is.EqualTo("Ana"), "a cloud save from a newer build is left alone");
            Assert.That(store.Status.LastError.Kind, Is.EqualTo(CloudErrorKind.Serialization));
        }

        [UnityTest]
        public IEnumerator ACloudSchemaWithoutAMigrationIsNotApplied() => Run.Wait(NoMigrationAsync());

        private async Awaitable NoMigrationAsync()
        {
            DataKey key = new("versioned");
            NewtonsoftSerializer serializer = new();
            SyncedDataStore store = NewStore(NewLocal());
            CloudPayload older = new() { savedAtUtcTicks = Clock.Now.Ticks, schemaVersion = 1, data = "{\"name\":\"Ana\",\"level\":4}" };
            Cloud.Seed(CloudKeyOf(key), serializer.Serialize(older, false));

            LoadResult<TestVersionedProfile> loaded = await store.LoadAsync<TestVersionedProfile>(key);

            Assert.That(loaded.Status, Is.EqualTo(DataStatus.NotFound));
            Assert.That(store.Status.LastError.Kind, Is.EqualTo(CloudErrorKind.Serialization));
        }

        [UnityTest]
        public IEnumerator LoadIntoAsyncFillsAnExistingObjectFromTheCloud() => Run.Wait(LoadIntoAsync());

        private async Awaitable LoadIntoAsync()
        {
            SyncedDataStore deviceA = NewStore(NewLocal());
            await deviceA.SaveAsync(_profile, new TestProfile("Ana", 6));

            DataStore localB = NewLocal();
            SyncedDataStore deviceB = NewStore(localB);
            TestProfile target = new("Blank", 0);
            LoadResult<TestProfile> loaded = await deviceB.LoadIntoAsync(_profile, target);

            Assert.That(loaded.Ok, Is.True);
            Assert.That(target.name, Is.EqualTo("Ana"));
            Assert.That(target.level, Is.EqualTo(6));
            Assert.That(localB.Load<TestProfile>(_profile).Value.level, Is.EqualTo(6));
        }

        [UnityTest]
        public IEnumerator AnUploadConflictFromAStaleWriteLockIsResolvedForTheNewerDeviceCopy() => Run.Wait(UploadConflictLocalAsync());

        private async Awaitable UploadConflictLocalAsync()
        {
            SyncedDataStore deviceA = NewStore(NewLocal());
            SyncedDataStore deviceB = NewStore(NewLocal());
            await deviceA.SaveAsync(_profile, new TestProfile("Ana", 1));
            await deviceB.LoadAsync<TestProfile>(_profile);

            Clock.Advance(_step);
            await deviceA.SaveAsync(_profile, new TestProfile("Ana", 2));
            Clock.Advance(_step);
            await deviceB.SaveAsync(_profile, new TestProfile("Ana", 3));

            Assert.That(deviceB.Status.ResolvedConflicts, Is.EqualTo(1));
            Assert.That(deviceB.PendingCount, Is.EqualTo(0));
            Assert.That(new NewtonsoftSerializer().Deserialize<TestProfile>(PayloadIn(Cloud, _profile).data).level, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator AnUploadConflictForAnOlderDeviceCopyDefersToTheCloudAndTheNextLoadDownloadsIt() => Run.Wait(UploadConflictCloudAsync());

        private async Awaitable UploadConflictCloudAsync()
        {
            SyncedDataStore deviceA = NewStore(NewLocal());
            SyncedDataStore deviceB = NewStore(NewLocal());
            await deviceA.SaveAsync(_profile, new TestProfile("Ana", 1));
            await deviceB.LoadAsync<TestProfile>(_profile);

            Clock.Advance(_step);
            deviceB.Save(_profile, new TestProfile("Ana", 3));
            Clock.Advance(_step);
            await deviceA.SaveAsync(_profile, new TestProfile("Ana", 2));

            await deviceB.FlushAsync();
            Assert.That(deviceB.PendingCount, Is.EqualTo(0), "the older device change was dropped, not uploaded");
            Assert.That(new NewtonsoftSerializer().Deserialize<TestProfile>(PayloadIn(Cloud, _profile).data).level, Is.EqualTo(2));

            LoadResult<TestProfile> loaded = await deviceB.LoadAsync<TestProfile>(_profile);
            Assert.That(loaded.Value.level, Is.EqualTo(2));
            Assert.That(deviceB.Load<TestProfile>(_profile).Value.level, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator ACancelledLoadThrowsAndLeavesTheStoreUsable() => Run.Wait(CancelledLoadAsync());

        private async Awaitable CancelledLoadAsync()
        {
            SyncedDataStore store = NewStore(NewLocal());
            using CancellationTokenSource source = new();
            source.Cancel();

            bool threw = false;
            try
            {
                await store.LoadAsync<TestProfile>(_profile, source.Token);
            }
            catch (OperationCanceledException)
            {
                threw = true;
            }

            Assert.That(threw, Is.True);
            LoadResult<TestProfile> loaded = await store.LoadAsync<TestProfile>(_profile);
            Assert.That(loaded.Status, Is.EqualTo(DataStatus.NotFound));
        }
    }
}
