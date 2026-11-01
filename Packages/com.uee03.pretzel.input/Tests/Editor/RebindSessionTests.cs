using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pretzel.Inputs.Tests
{
    public sealed class RebindSessionTests : InputTestFixture
    {
        private InputActionAsset _asset;
        private Keyboard _keyboard;
        private InputAction _jump;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _asset = TestActions.Create();
            _jump = _asset.FindAction("Gameplay/Jump");
            _keyboard = InputSystem.AddDevice<Keyboard>();
        }

        [TearDown]
        public override void TearDown()
        {
            _asset.Disable();
            Object.DestroyImmediate(_asset);
            base.TearDown();
        }

        private void Settle()
        {
            currentTime += 0.5;
            InputSystem.Update();
        }

        [Test]
        public void PressingAControl_RebindsTheBinding()
        {
            RebindResult? result = null;
            using RebindSession session = RebindSession.Start(_jump, 0, r => result = r);

            Press(_keyboard.jKey);
            Release(_keyboard.jKey);
            Settle();

            Assert.AreEqual(RebindResult.Completed, result);
            Assert.AreEqual("<Keyboard>/j", _jump.bindings[0].effectivePath);
            Assert.IsFalse(session.IsRunning);
        }

        [Test]
        public void Escape_CancelsAndLeavesTheBindingAlone()
        {
            RebindResult? result = null;
            using RebindSession session = RebindSession.Start(_jump, 0, r => result = r);

            Press(_keyboard.escapeKey);
            Release(_keyboard.escapeKey);
            Settle();

            Assert.AreEqual(RebindResult.Cancelled, result);
            Assert.AreEqual("<Keyboard>/space", _jump.bindings[0].effectivePath);
        }

        [Test]
        public void AnEnabledAction_IsDisabledWhileWaiting_AndEnabledAgainAfterwards()
        {
            _asset.FindActionMap("Gameplay").Enable();
            RebindSession session = RebindSession.Start(_jump, 0, _ => { });
            Assert.IsFalse(_jump.enabled, "The Input System requires the action to be disabled during a rebind.");

            session.Cancel();
            Settle();

            Assert.IsTrue(_jump.enabled);
            session.Dispose();
        }

        [Test]
        public void Dispose_CancelsASessionWithoutCallingBack()
        {
            int calls = 0;
            RebindSession session = RebindSession.Start(_jump, 0, _ => calls++);

            session.Dispose();
            Press(_keyboard.jKey);
            Release(_keyboard.jKey);
            Settle();

            Assert.AreEqual(0, calls);
            Assert.AreEqual("<Keyboard>/space", _jump.bindings[0].effectivePath);
            Assert.DoesNotThrow(() => session.Dispose());
        }

        [Test]
        public void Start_RejectsABadBindingIndexOrANullAction()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => RebindSession.Start(_jump, 9, _ => { }));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => RebindSession.Start(_jump, -1, _ => { }));
            Assert.Throws<System.ArgumentNullException>(() => RebindSession.Start(null, 0, _ => { }));
        }
    }
}
