using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pretzel.Inputs.Tests
{
    public sealed class BindingPersistenceTests
    {
        private sealed class MemoryStore : IBindingStore
        {
            public string Json = string.Empty;

            public string Load() => Json;

            public void Save(string json) => Json = json;
        }

        private InputActionAsset _asset;
        private InputActionAsset _other;

        [SetUp]
        public void SetUp()
        {
            _asset = TestActions.Create();

            // The same asset loaded again, which is what a new session sees: overrides are matched by binding id.
            _other = InputActionAsset.FromJson(_asset.ToJson());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_asset);
            Object.DestroyImmediate(_other);
        }

        private static string Path(InputActionAsset asset) => asset.FindAction("Gameplay/Jump").bindings[0].effectivePath;

        [Test]
        public void ARebind_SavedInOneAsset_AppliesToAnotherSession()
        {
            MemoryStore store = new();
            _asset.FindAction("Gameplay/Jump").ApplyBindingOverride(0, "<Keyboard>/j");

            BindingPersistence.Save(_asset, store);
            BindingPersistence.Apply(_other, store);

            Assert.AreEqual("<Keyboard>/j", Path(_other));
        }

        [Test]
        public void Apply_WithNothingSaved_KeepsTheDefaults()
        {
            BindingPersistence.Apply(_asset, new MemoryStore());

            Assert.AreEqual("<Keyboard>/space", Path(_asset));
        }

        [Test]
        public void Reset_RestoresTheDefaultsAndClearsTheStore()
        {
            MemoryStore store = new();
            _asset.FindAction("Gameplay/Jump").ApplyBindingOverride(0, "<Keyboard>/j");
            BindingPersistence.Save(_asset, store);

            BindingPersistence.Reset(_asset, store);

            Assert.AreEqual("<Keyboard>/space", Path(_asset));
            Assert.IsEmpty(store.Json);
        }

        [Test]
        public void Save_OnlyKeepsWhatWasChanged()
        {
            MemoryStore store = new();
            _asset.FindAction("Gameplay/Jump").ApplyBindingOverride(1, "<Gamepad>/buttonEast");

            BindingPersistence.Save(_asset, store);
            BindingPersistence.Apply(_other, store);

            Assert.AreEqual("<Keyboard>/space", Path(_other));
            Assert.AreEqual("<Gamepad>/buttonEast", _other.FindAction("Gameplay/Jump").bindings[1].effectivePath);
        }

        [Test]
        public void NullArguments_AreRejected()
        {
            Assert.Throws<System.ArgumentNullException>(() => BindingPersistence.Apply(null, new MemoryStore()));
            Assert.Throws<System.ArgumentNullException>(() => BindingPersistence.Save(_asset, null));
            Assert.Throws<System.ArgumentNullException>(() => BindingPersistence.Reset(null, null));
        }

        [Test]
        public void PlayerPrefsStore_RoundTripsAndDeletesOnEmpty()
        {
            string key = "pretzel.input.tests." + System.Guid.NewGuid().ToString("N");
            PlayerPrefsBindingStore store = new(key);
            try
            {
                Assert.IsEmpty(store.Load());

                store.Save("{\"x\":1}");
                Assert.AreEqual("{\"x\":1}", store.Load());

                store.Save(string.Empty);
                Assert.IsFalse(PlayerPrefs.HasKey(key));
            }
            finally
            {
                PlayerPrefs.DeleteKey(key);
            }
        }
    }
}
