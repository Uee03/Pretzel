using System.Collections.Generic;
using NUnit.Framework;
using Pretzel.Cloud.InMemory;

namespace Pretzel.Cloud.Tests
{
    public sealed class InMemoryCloudSaveServiceTests
    {
        [Test]
        public void SaveThenLoadReturnsTheValueAndALock()
        {
            InMemoryCloudSaveService service = new();
            CloudResult<string> saved = Run.Result(service.SaveAsync("k", "v1"));
            Assert.That(saved.IsSuccess, Is.True);
            CloudResult<CloudSaveItem> loaded = Run.Result(service.LoadAsync("k"));
            Assert.That(loaded.Value.Value, Is.EqualTo("v1"));
            Assert.That(loaded.Value.WriteLock, Is.EqualTo(saved.Value));
        }

        [Test]
        public void MissingKeyIsNotFound()
        {
            InMemoryCloudSaveService service = new();
            Assert.That(Run.Result(service.LoadAsync("nope")).Error.Kind, Is.EqualTo(CloudErrorKind.NotFound));
        }

        [Test]
        public void WriteLockMismatchIsAConflictAndMatchingLockWins()
        {
            InMemoryCloudSaveService service = new();
            string first = Run.Result(service.SaveAsync("k", "v1")).Value;
            string second = Run.Result(service.SaveAsync("k", "v2", first)).Value;
            Assert.That(second, Is.Not.EqualTo(first));

            CloudResult<string> stale = Run.Result(service.SaveAsync("k", "v3", first));
            Assert.That(stale.Error.Kind, Is.EqualTo(CloudErrorKind.Conflict));
            Assert.That(service.TryGetValue("k", out string value), Is.True);
            Assert.That(value, Is.EqualTo("v2"));
        }

        [Test]
        public void WritingWithALockToAMissingKeyIsAConflict()
        {
            InMemoryCloudSaveService service = new();
            Assert.That(Run.Result(service.SaveAsync("k", "v", "1")).Error.Kind, Is.EqualTo(CloudErrorKind.Conflict));
        }

        [Test]
        public void DeleteChecksTheLockAndIgnoresMissingKeys()
        {
            InMemoryCloudSaveService service = new();
            string writeLock = Run.Result(service.SaveAsync("k", "v")).Value;
            Assert.That(Run.Result(service.DeleteAsync("k", "wrong")).Error.Kind, Is.EqualTo(CloudErrorKind.Conflict));
            Assert.That(Run.Result(service.DeleteAsync("k", writeLock)).IsSuccess, Is.True);
            Assert.That(Run.Result(service.DeleteAsync("k", "anything")).IsSuccess, Is.True);
            Assert.That(service.Keys, Is.Empty);
        }

        [Test]
        public void ListKeysReturnsEveryKey()
        {
            InMemoryCloudSaveService service = new();
            service.Seed("a", "1");
            service.Seed("b", "2");
            IReadOnlyList<CloudSaveKeyInfo> keys = Run.Result(service.ListKeysAsync()).Value;
            Assert.That(keys.Count, Is.EqualTo(2));
        }

        [Test]
        public void InjectedFaultsFailTheNextCallsThenClear()
        {
            InMemoryCloudSaveService service = new();
            service.Faults.Enqueue(CloudErrorKind.RateLimited, 2);
            Assert.That(Run.Result(service.SaveAsync("k", "v")).Error.Kind, Is.EqualTo(CloudErrorKind.RateLimited));
            Assert.That(Run.Result(service.LoadAsync("k")).Error.Kind, Is.EqualTo(CloudErrorKind.RateLimited));
            Assert.That(Run.Result(service.SaveAsync("k", "v")).IsSuccess, Is.True);
            Assert.That(service.Faults.CallCount, Is.EqualTo(3));
        }

        [Test]
        public void OfflineFailsEverythingUntilItIsCleared()
        {
            InMemoryCloudSaveService service = new();
            service.Faults.Offline = true;
            Assert.That(Run.Result(service.SaveAsync("k", "v")).Error.Kind, Is.EqualTo(CloudErrorKind.Offline));
            Assert.That(Run.Result(service.ListKeysAsync()).Error.Kind, Is.EqualTo(CloudErrorKind.Offline));
            service.Faults.Clear();
            Assert.That(Run.Result(service.SaveAsync("k", "v")).IsSuccess, Is.True);
        }

        [Test]
        public void CancelledCallsThrow()
        {
            InMemoryCloudSaveService service = new();
            using System.Threading.CancellationTokenSource source = new();
            source.Cancel();
            Assert.Throws<System.OperationCanceledException>(() => Run.Result(service.LoadAsync("k", source.Token)));
            Assert.That(service.LoadCount, Is.EqualTo(0));
        }
    }
}
