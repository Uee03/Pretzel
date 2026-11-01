using System.IO;
using NUnit.Framework;
using Pretzel.Data;
using UnityEngine;

namespace Pretzel.Samples.NoContainer.Tests
{
    public sealed class GameServicesTests
    {
        private string _folder;
        private DataStore _store;
        private GameServices _services;
        private float _listenerVolume;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "pretzel-nc-" + System.Guid.NewGuid().ToString("N"));
            _store = new DataStore(DataRoots.Absolute(_folder));
            _services = ScriptableObject.CreateInstance<GameServices>();
            _listenerVolume = AudioListener.volume;
        }

        [TearDown]
        public void TearDown()
        {
            _services.Shutdown();
            Object.DestroyImmediate(_services);
            AudioListener.volume = _listenerVolume;
            if (Directory.Exists(_folder))
            {
                Directory.Delete(_folder, true);
            }
        }

        [Test]
        public void Initialize_BuildsEveryService()
        {
            _services.Initialize(_store);

            Assert.IsTrue(_services.IsInitialized);
            Assert.IsNotNull(_services.Events);
            Assert.IsNotNull(_services.Scenes);
            Assert.IsNotNull(_services.Progress);
            Assert.IsNotNull(_services.Preferences);
        }

        [Test]
        public void Initialize_Twice_ReplacesTheServicesAndDropsOldSubscribers()
        {
            _services.Initialize(_store);
            int received = 0;
            _services.Events.Subscribe<ScoreChanged>(_ => received++);
            Pretzel.Events.IEventBus first = _services.Events;

            _services.Initialize(_store);
            first.Publish(new ScoreChanged(1));
            _services.Events.Publish(new ScoreChanged(2));

            Assert.AreNotSame(first, _services.Events);
            Assert.AreEqual(0, received);
        }

        [Test]
        public void Shutdown_SavesPendingProgressAndClearsTheServices()
        {
            _services.Initialize(_store);
            _services.Progress.RecordRound(9);

            _services.Shutdown();

            Assert.IsFalse(_services.IsInitialized);
            Assert.IsNull(_services.Progress);
            Assert.IsTrue(_store.TryLoad(ProgressService.SaveKey, out GameProgress saved));
            Assert.AreEqual(9, saved.bestScore);
        }

        [Test]
        public void Shutdown_WithoutInitialize_DoesNothing()
        {
            Assert.DoesNotThrow(() => _services.Shutdown());
        }

        [Test]
        public void MasterVolume_DrivesTheAudioListener()
        {
            _services.Initialize(_store);

            _services.Preferences.Modify(p => p.audio.master = 0.25f);
            Assert.AreEqual(0.25f, AudioListener.volume, 0.0001f);

            _services.Preferences.Modify(p => p.audio.muted = true);
            Assert.AreEqual(0f, AudioListener.volume);
        }
    }
}
