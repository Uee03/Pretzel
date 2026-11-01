using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pretzel.Inputs.Tests
{
    public sealed class InputRouterTests : InputTestFixture
    {
        private InputActionAsset _asset;
        private Keyboard _keyboard;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _asset = TestActions.Create();
            _keyboard = InputSystem.AddDevice<Keyboard>();
        }

        [TearDown]
        public override void TearDown()
        {
            Object.DestroyImmediate(_asset);
            base.TearDown();
        }

        private bool Enabled(string map) => _asset.FindActionMap(map).enabled;

        [Test]
        public void WithNoContext_EveryMapIsDisabled()
        {
            using InputRouter router = new(_asset);

            Assert.IsFalse(Enabled("Gameplay"));
            Assert.IsFalse(Enabled("Menu"));
            Assert.IsFalse(Enabled("Debug"));
        }

        [Test]
        public void ThePushedContext_IsTheOnlyEnabledMap()
        {
            using InputRouter router = new(_asset);

            router.Contexts.Push("Gameplay");

            Assert.IsTrue(Enabled("Gameplay"));
            Assert.IsFalse(Enabled("Menu"));
            Assert.AreEqual("Gameplay", router.ActiveContext);
        }

        [Test]
        public void ANewContext_ReplacesTheOldOneAndPoppingBringsItBack()
        {
            using InputRouter router = new(_asset);
            router.Contexts.Push("Gameplay");

            router.Contexts.Push("Menu");
            Assert.IsFalse(Enabled("Gameplay"));
            Assert.IsTrue(Enabled("Menu"));

            router.Contexts.Remove("Menu");
            Assert.IsTrue(Enabled("Gameplay"));
            Assert.IsFalse(Enabled("Menu"));
        }

        [Test]
        public void Blocking_DisablesEveryMapAndUnblockingRestoresTheContext()
        {
            using InputRouter router = new(_asset);
            router.Contexts.Push("Gameplay");

            router.Blockers.Block("scene-load");
            Assert.IsFalse(Enabled("Gameplay"));
            Assert.IsNull(router.ActiveContext);

            router.Blockers.Unblock("scene-load");
            Assert.IsTrue(Enabled("Gameplay"));
        }

        [Test]
        public void TheAlwaysOnMap_StaysEnabledInEveryContext_ButNotWhileBlocked()
        {
            using InputRouter router = new(_asset, alwaysOnMap: "Debug");

            Assert.IsTrue(Enabled("Debug"));
            router.Contexts.Push("Menu");
            Assert.IsTrue(Enabled("Debug"));

            router.Blockers.Block("cutscene");
            Assert.IsFalse(Enabled("Debug"));
        }

        [Test]
        public void ABlockedAction_DoesNotFire_AndAnUnblockedOneDoes()
        {
            using InputRouter router = new(_asset);
            router.Contexts.Push("Gameplay");
            int performed = 0;
            _asset.FindAction("Gameplay/Jump").performed += _ => performed++;

            router.Blockers.Block("scene-load");
            Press(_keyboard.spaceKey);
            Release(_keyboard.spaceKey);
            Assert.AreEqual(0, performed, "Input must not reach the game while blocked.");

            router.Blockers.Unblock("scene-load");
            Press(_keyboard.spaceKey);
            Release(_keyboard.spaceKey);
            Assert.AreEqual(1, performed);
        }

        [Test]
        public void AnActionOfAnInactiveContext_DoesNotFire()
        {
            using InputRouter router = new(_asset);
            router.Contexts.Push("Menu");
            int jumped = 0;
            _asset.FindAction("Gameplay/Jump").performed += _ => jumped++;

            Press(_keyboard.spaceKey);

            Assert.AreEqual(0, jumped);
        }

        [Test]
        public void Dispose_DisablesTheMapsAndIgnoresLaterChanges()
        {
            InputRouter router = new(_asset);
            router.Contexts.Push("Gameplay");

            router.Dispose();
            router.Contexts.Push("Menu");

            Assert.IsFalse(Enabled("Gameplay"));
            Assert.IsFalse(Enabled("Menu"));
            Assert.DoesNotThrow(() => router.Dispose());
        }

        [Test]
        public void Refresh_IsHarmlessWhenNothingChanged()
        {
            using InputRouter router = new(_asset);
            router.Contexts.Push("Gameplay");

            Assert.DoesNotThrow(() =>
            {
                router.Refresh();
                router.Refresh();
            });
            Assert.IsTrue(Enabled("Gameplay"));
        }

        [Test]
        public void TheConstructor_RejectsANullAsset()
        {
            Assert.Throws<System.ArgumentNullException>(() => new InputRouter(null));
        }
    }
}
