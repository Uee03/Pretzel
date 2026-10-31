using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Pretzel.Cloud.InMemory;
using Pretzel.Data;

namespace Pretzel.Cloud.Tests
{
    /// <summary>
    /// Builds devices (a real <see cref="DataStore"/> in a temp folder plus a <see cref="SyncedDataStore"/>) that share one in-memory cloud.
    /// A fake clock and a recording zero delay keep the tests fast and deterministic. Nothing talks to a network.
    /// </summary>
    public abstract class SyncedStoreTestBase
    {
        private readonly List<string> _folders = new();

        internal InMemoryCloudSaveService Cloud { get; private set; }

        internal TestClock Clock { get; private set; }

        internal RecordingDelay Delay { get; private set; }

        [SetUp]
        public void BaseSetUp()
        {
            Clock = new TestClock();
            Delay = new RecordingDelay();
            Cloud = new InMemoryCloudSaveService(Clock.Read);
        }

        [TearDown]
        public void BaseTearDown()
        {
            foreach (string folder in _folders)
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }

            _folders.Clear();
        }

        protected string NewFolder()
        {
            string folder = Path.Combine(Path.GetTempPath(), "PretzelCloud_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            _folders.Add(folder);
            return folder;
        }

        protected DataStore NewLocal(string folder = null)
        {
            return new DataStore(DataRoots.Absolute(folder ?? NewFolder()));
        }

        internal SyncedDataStore NewStore(IDataStore local, Action<SyncedDataStoreOptions> configure = null)
        {
            SyncedDataStoreOptions options = new()
            {
                Clock = Clock.Read,
                RetryDelay = Delay,
                Random = () => 0.5d,
                FlushRetry = new RetryPolicy(3, TimeSpan.FromSeconds(1), 2d, TimeSpan.FromSeconds(30), 0d)
            };
            configure?.Invoke(options);
            return new SyncedDataStore(local, Cloud, options);
        }

        protected static string CloudKeyOf(DataKey key) => CloudKeyMapper.ToCloudKey(key);

        /// <summary>The save time a device would have written into the cloud payload for <paramref name="key"/>, read back from the cloud.</summary>
        internal static CloudPayload PayloadIn(InMemoryCloudSaveService cloud, DataKey key)
        {
            Assert.That(cloud.TryGetValue(CloudKeyMapper.ToCloudKey(key), out string text), Is.True, "no cloud item for " + key);
            return new NewtonsoftSerializer().Deserialize<CloudPayload>(text);
        }
    }
}
