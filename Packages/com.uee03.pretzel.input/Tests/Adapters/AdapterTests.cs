using System.IO;
using NUnit.Framework;
using Pretzel.Data;
using Pretzel.Inputs.Preferences;
using Pretzel.Inputs.Scenes;
using Pretzel.Preferences;
using Pretzel.Scenes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pretzel.Inputs.Adapters.Tests
{
    public sealed class AdapterTests
    {
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "pretzel-input-" + System.Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folder))
            {
                Directory.Delete(_folder, true);
            }
        }

        [Test]
        public void PreferencesStore_SavesRebindsWithTheOtherControlSettings()
        {
            DataStore data = new(DataRoots.Absolute(_folder));
            PreferencesService preferences = new(data);
            PreferencesBindingStore store = new(preferences);

            store.Save("{\"overrides\":[]}");

            PreferencesService reloaded = new(data);
            reloaded.Load();
            Assert.AreEqual("{\"overrides\":[]}", reloaded.Current.controls.bindingOverridesJson);
            Assert.AreEqual("{\"overrides\":[]}", new PreferencesBindingStore(reloaded).Load());
        }

        [Test]
        public void PreferencesStore_LoadsAnEmptyStringWhenNothingIsSaved()
        {
            PreferencesBindingStore store = new(new PreferencesService(new DataStore(DataRoots.Absolute(_folder))));

            Assert.IsEmpty(store.Load());
        }

        [Test]
        public void PreferencesStore_RejectsANullService()
        {
            Assert.Throws<System.ArgumentNullException>(() => new PreferencesBindingStore(null));
        }

        [Test]
        public void SceneLoadInputGate_RejectsNulls_AndReleasesItsBlockOnDispose()
        {
            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            try
            {
                using InputRouter router = new(asset);
                SceneLoader loader = new();

                Assert.Throws<System.ArgumentNullException>(() => new SceneLoadInputGate(null, router));
                Assert.Throws<System.ArgumentNullException>(() => new SceneLoadInputGate(loader, null));

                SceneLoadInputGate gate = new(loader, router);
                router.Blockers.Block(SceneLoadInputGate.Reason);
                gate.Dispose();

                Assert.IsFalse(router.Blockers.IsBlocked);
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }
    }
}
