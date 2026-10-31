using System;
using System.Collections;
using NUnit.Framework;
using Pretzel.Cloud.InMemory;
using Pretzel.Data;
using UnityEngine;
using UnityEngine.TestTools;

namespace Pretzel.Cloud.Tests
{
    public sealed class CloudServicesTests : SyncedStoreTestBase
    {
        [Test]
        public void TheBuilderWiresWhateverWasAdded()
        {
            InMemoryCloudAuthService auth = new();
            InMemoryCloudSaveService save = new();
            NewtonsoftSerializer serializer = new();

            CloudServices services = CloudServices.CreateBuilder(serializer).UseAuth(auth).UseSave(save).Build();

            Assert.That(services.Auth, Is.SameAs(auth));
            Assert.That(services.Save, Is.SameAs(save));
            Assert.That(services.Code, Is.Null);
            Assert.That(services.RemoteConfig, Is.Null);
            Assert.That(services.Serializer, Is.SameAs(serializer));
        }

        [Test]
        public void TheBuilderDefaultsToTheNewtonsoftSerializer()
        {
            Assert.That(CloudServices.CreateBuilder().Serializer, Is.TypeOf<NewtonsoftSerializer>());
        }

        [Test]
        public void ServicesCannotBeNull()
        {
            CloudServicesBuilder builder = CloudServices.CreateBuilder();
            Assert.Throws<ArgumentNullException>(() => builder.UseAuth(null));
            Assert.Throws<ArgumentNullException>(() => builder.UseSave(null));
            Assert.Throws<ArgumentNullException>(() => builder.UseCode(null));
            Assert.Throws<ArgumentNullException>(() => builder.UseRemoteConfig(null));
        }

        [Test]
        public void ConnectSignsInAnonymously()
        {
            InMemoryCloudAuthService auth = new("p1");
            CloudServices services = CloudServices.CreateBuilder().UseAuth(auth).Build();

            CloudResult result = Run.Result(services.ConnectAsync());

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(auth.PlayerId, Is.EqualTo("p1"));
        }

        [Test]
        public void ConnectRetriesTransientFailuresWithTheBuilderPolicy()
        {
            InMemoryCloudAuthService auth = new();
            auth.Faults.Enqueue(CloudErrorKind.Offline, 2);
            CloudServices services = CloudServices.CreateBuilder().UseAuth(auth).WithRetry(new RetryPolicy(4, TimeSpan.FromSeconds(1), 2d, TimeSpan.FromSeconds(30), 0d), Delay).Build();

            Assert.That(Run.Result(services.ConnectAsync()).IsSuccess, Is.True);
            Assert.That(Delay.Delays, Is.EqualTo(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2) }));
        }

        [Test]
        public void ConnectWithoutAnAuthServiceFails()
        {
            CloudServices services = CloudServices.CreateBuilder().Build();
            Assert.That(Run.Result(services.ConnectAsync()).Error.Kind, Is.EqualTo(CloudErrorKind.NotInitialized));
        }

        [UnityTest]
        public IEnumerator ASyncedStoreFromTheServicesRoutesDataIOLikeAnyStore() => Run.Wait(SyncedStoreAsync());

        private async Awaitable SyncedStoreAsync()
        {
            CloudServices services = CloudServices.CreateBuilder().UseSave(Cloud).Build();
            SyncedDataStoreOptions options = new() { Clock = Clock.Read, RetryDelay = Delay };
            SyncedDataStore store = services.CreateSyncedStore(NewLocal(), options);

            IDataStore asDataStore = store;
            await asDataStore.SaveAsync(new DataKey("profile"), new TestProfile("Ana", 1));

            Assert.That(Cloud.Keys.Count, Is.EqualTo(1));
        }

        [Test]
        public void ASyncedStoreNeedsACloudSaveService()
        {
            CloudServices services = CloudServices.CreateBuilder().Build();
            Assert.Throws<InvalidOperationException>(() => services.CreateSyncedStore(NewLocal()));
        }
    }
}
