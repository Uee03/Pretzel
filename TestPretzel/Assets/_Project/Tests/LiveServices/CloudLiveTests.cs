using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Pretzel.Cloud;
using UnityEngine;
using UnityEngine.TestTools;

namespace Pretzel.TestPretzel.LiveServicesTests
{
    [Category(LiveServices.Category)]
    public sealed class CloudLiveTests
    {
        private const string Key = "pretzel-live-test";

        private CloudServices _services;
        private CancellationTokenSource _timeout;

        [SetUp]
        public void SetUp()
        {
            LiveServices.RequireLinkedProject();
            _timeout = new CancellationTokenSource(LiveServices.Timeout);
            _services = CloudServices.CreateBuilder()
                .UseUnityAuthentication()
                .UseUnityCloudSave()
                .UseUnityCloudCode()
                .Build();
        }

        [TearDown]
        public void TearDown()
        {
            _timeout?.Dispose();
        }

        [Test]
        public async Task Connect_SignsInAnonymouslyAsync()
        {
            CloudResult connected = await _services.ConnectAsync(_timeout.Token);

            Assert.IsTrue(connected.IsSuccess, connected.ToString());
            Assert.IsTrue(_services.Auth.IsSignedIn);
            Assert.IsFalse(string.IsNullOrEmpty(_services.Auth.PlayerId));
            Debug.Log($"[LiveServices] Signed in as {_services.Auth.PlayerId}.");
        }

        [Test]
        public async Task CloudSave_SaveLoadDelete_RoundTripsAsync()
        {
            await ConnectAsync();
            string value = "live " + Guid.NewGuid().ToString("N");

            CloudResult<string> first = await _services.Save.SaveAsync(Key, "first", null, _timeout.Token);
            Assert.IsTrue(first.IsSuccess, first.ToString());
            CloudResult<string> saved = await _services.Save.SaveAsync(Key, value, first.Value, _timeout.Token);
            Assert.IsTrue(saved.IsSuccess, saved.ToString());

            CloudResult<CloudSaveItem> loaded = await _services.Save.LoadAsync(Key, _timeout.Token);
            Assert.IsTrue(loaded.IsSuccess, loaded.ToString());
            Assert.AreEqual(value, loaded.Value.Value);
            Assert.AreEqual(saved.Value, loaded.Value.WriteLock);

            CloudResult stale = await _services.Save.DeleteAsync(Key, first.Value, _timeout.Token);
            Assert.AreEqual(CloudErrorKind.Conflict, stale.Error?.Kind, stale.ToString());

            CloudResult deleted = await _services.Save.DeleteAsync(Key, loaded.Value.WriteLock, _timeout.Token);
            Assert.IsTrue(deleted.IsSuccess, deleted.ToString());

            CloudResult<CloudSaveItem> missing = await _services.Save.LoadAsync(Key, _timeout.Token);
            Assert.AreEqual(CloudErrorKind.NotFound, missing.Error?.Kind, missing.ToString());
        }

        [Test]
        public async Task CloudCode_MissingModule_FailsAsAResultInsteadOfThrowingAsync()
        {
            await ConnectAsync();
            // The Cloud Code SDK logs the failure itself before Pretzel turns it into a result.
            LogAssert.Expect(LogType.Error, new Regex("Module could not be found"));

            CloudResult<string> called = await _services.Code.CallModuleAsync<string, string>("PretzelLiveTestMissingModule", "Missing", null, _timeout.Token);

            Assert.AreEqual(CloudErrorKind.NotFound, called.Error?.Kind, called.ToString());
            Debug.Log($"[LiveServices] Missing Cloud Code module reported: {called.Error} (code {called.Error.Code}).");
        }

        private async Task ConnectAsync()
        {
            CloudResult connected = await _services.ConnectAsync(_timeout.Token);
            Assert.IsTrue(connected.IsSuccess, connected.ToString());
        }
    }
}
