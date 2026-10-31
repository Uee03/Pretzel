using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Internal;
using Unity.Services.CloudSave.Models;
using UnityEngine;
using PlayerDeleteOptions = Unity.Services.CloudSave.Models.Data.Player.DeleteOptions;

namespace Pretzel.Cloud.CloudSave.Tests
{
    /// <summary>Drives <see cref="UnityCloudSaveService"/> against a stand-in for the SDK's player data service. No Unity Services run.</summary>
    public sealed class UnityCloudSaveServiceTests
    {
        /// <summary>Answers calls on <see cref="IPlayerDataService"/> through a delegate, so the test does not implement its many members.</summary>
        public class PlayerDataProxy : DispatchProxy
        {
            public Func<MethodInfo, object[], object> Handler { get; set; }

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                return Handler(targetMethod, args);
            }
        }

        private static UnityCloudSaveService Create(Func<MethodInfo, object[], object> handler)
        {
            IPlayerDataService proxy = DispatchProxy.Create<IPlayerDataService, PlayerDataProxy>();
            ((PlayerDataProxy)proxy).Handler = handler;
            return new UnityCloudSaveService(() => proxy);
        }

        private static T Result<T>(Awaitable<T> awaitable)
        {
            Awaitable<T>.Awaiter awaiter = awaitable.GetAwaiter();
            Assert.That(awaiter.IsCompleted, Is.True, "the call should complete without waiting");
            return awaiter.GetResult();
        }

        [Test]
        public void SaveSendsTheValueAndTheWriteLockAndReturnsTheNewLock()
        {
            IDictionary<string, SaveItem> sent = null;
            UnityCloudSaveService service = Create((method, args) =>
            {
                Assert.That(method.Name, Is.EqualTo("SaveAsync"));
                sent = (IDictionary<string, SaveItem>)args[0];
                return Task.FromResult(new Dictionary<string, string>(StringComparer.Ordinal) { ["k"] = "lock-2" });
            });

            CloudResult<string> result = Result(service.SaveAsync("k", "payload", "lock-1"));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo("lock-2"));
            Assert.That(sent["k"].Value, Is.EqualTo("payload"));
            Assert.That(sent["k"].WriteLock, Is.EqualTo("lock-1"));
        }

        [Test]
        public void SaveWithoutALockSendsNoWriteLock()
        {
            IDictionary<string, SaveItem> sent = null;
            UnityCloudSaveService service = Create((method, args) =>
            {
                sent = (IDictionary<string, SaveItem>)args[0];
                return Task.FromResult(new Dictionary<string, string>(StringComparer.Ordinal) { ["k"] = "lock-1" });
            });

            Result(service.SaveAsync("k", "payload"));

            Assert.That(sent["k"].WriteLock, Is.Null);
        }

        [Test]
        public void DeleteSendsTheWriteLockInTheOptions()
        {
            PlayerDeleteOptions sent = null;
            string deleted = null;
            UnityCloudSaveService service = Create((method, args) =>
            {
                Assert.That(method.Name, Is.EqualTo("DeleteAsync"));
                deleted = (string)args[0];
                sent = (PlayerDeleteOptions)args[1];
                return Task.CompletedTask;
            });

            CloudResult result = Result(service.DeleteAsync("k", "lock-1"));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(deleted, Is.EqualTo("k"));
            Assert.That(sent.WriteLock, Is.EqualTo("lock-1"));
        }

        [Test]
        public void ASdkExceptionBecomesAFailedResult()
        {
            UnityCloudSaveService service = Create((method, args) => Task.FromException<Dictionary<string, string>>(
                (Exception)Activator.CreateInstance(typeof(CloudSaveConflictException), BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { CloudSaveExceptionReason.Conflict, 409, "stale lock", null, null }, null)));

            CloudResult<string> result = Result(service.SaveAsync("k", "payload", "old"));

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error.Kind, Is.EqualTo(CloudErrorKind.Conflict));
            Assert.That(result.Error.Code, Is.EqualTo(409));
        }

        [Test]
        public void AServiceThatIsNotInitializedIsReportedNotThrown()
        {
            UnityCloudSaveService service = new(() => throw new Unity.Services.Core.ServicesInitializationException("not initialized"));
            CloudResult<CloudSaveItem> result = Result(service.LoadAsync("k"));
            Assert.That(result.Error.Kind, Is.EqualTo(CloudErrorKind.NotInitialized));
        }

        [Test]
        public void AnEmptyLoadResponseIsNotFound()
        {
            UnityCloudSaveService service = Create((method, args) => Task.FromResult(new Dictionary<string, Item>(StringComparer.Ordinal)));
            Assert.That(Result(service.LoadAsync("k")).Error.Kind, Is.EqualTo(CloudErrorKind.NotFound));
        }

        [Test]
        public void ACancelledTokenThrowsBeforeTheSdkIsCalled()
        {
            int calls = 0;
            UnityCloudSaveService service = Create((method, args) =>
            {
                calls++;
                return Task.CompletedTask;
            });
            using CancellationTokenSource source = new();
            source.Cancel();

            Assert.Throws<OperationCanceledException>(() => Result(service.DeleteAsync("k", null, source.Token)));
            Assert.That(calls, Is.EqualTo(0));
        }

        [Test]
        public void TheBuilderExtensionAddsTheService()
        {
            CloudServices services = CloudServices.CreateBuilder().UseUnityCloudSave().Build();
            Assert.That(services.Save, Is.TypeOf<UnityCloudSaveService>());
        }
    }
}
