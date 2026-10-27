using System;
using System.IO;
using NUnit.Framework;
using Pretzel.Data;

namespace Pretzel.Preferences.Tests
{
    public sealed class PreferencesServiceTests
    {
        private string _folder;
        private DataStore _store;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "PretzelPrefs_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            _store = new DataStore(DataRoots.Absolute(_folder));
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_folder, true);

        [Test]
        public void MissingFileGivesDefaults()
        {
            PreferencesService service = new(_store);
            int changed = 0;
            service.Changed += _ => changed++;
            LoadResult<PlayerPreferences> result = service.Load();
            Assert.That(result.Status, Is.EqualTo(DataStatus.NotFound));
            Assert.That(service.Current.audio.music, Is.EqualTo(0.8f));
            Assert.That(changed, Is.EqualTo(1));
        }

        [Test]
        public void ModifiedPreferencesRoundTrip()
        {
            PreferencesService service = new(_store);
            service.Modify(p =>
            {
                p.audio.master = 0.25f;
                p.controls.invertY = true;
                p.graphics.targetFrameRate = 60;
                p.language = "pt-BR";
            }, save: true);

            PreferencesService reloaded = new(_store);
            Assert.That(reloaded.Load().Ok, Is.True);
            Assert.That(reloaded.Current.audio.master, Is.EqualTo(0.25f));
            Assert.That(reloaded.Current.controls.invertY, Is.True);
            Assert.That(reloaded.Current.graphics.targetFrameRate, Is.EqualTo(60));
            Assert.That(reloaded.Current.language, Is.EqualTo("pt-BR"));
        }

        [Test]
        public void CorruptFileFallsBackToDefaultsAndReportsIt()
        {
            File.WriteAllText(Path.Combine(_folder, "preferences.json"), "{ not json");
            PreferencesService service = new(_store);
            LoadResult<PlayerPreferences> result = service.Load();
            Assert.That(result.Status, Is.EqualTo(DataStatus.Corrupt));
            Assert.That(service.Current, Is.Not.Null);
            Assert.That(service.Current.audio.master, Is.EqualTo(1f));
        }

        [Test]
        public void CloneIsDeep()
        {
            PlayerPreferences original = new();
            PlayerPreferences copy = original.Clone();
            copy.audio.master = 0f;
            copy.controls.bindingOverridesJson = "{}";
            Assert.That(original.audio.master, Is.EqualTo(1f));
            Assert.That(original.controls.bindingOverridesJson, Is.Empty);
        }

        [Test]
        public void ResetToDefaultsReplacesCurrent()
        {
            PreferencesService service = new(_store);
            service.Modify(p => p.audio.sfx = 0f);
            service.ResetToDefaults();
            Assert.That(service.Current.audio.sfx, Is.EqualTo(1f));
        }
    }
}
