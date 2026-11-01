using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pretzel.Inputs.Tests
{
    public sealed class InputDeviceTrackerTests : InputTestFixture
    {
        private InputActionAsset _asset;
        private Keyboard _keyboard;
        private Gamepad _gamepad;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _asset = TestActions.Create();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            _asset.FindActionMap("Gameplay").Enable();
        }

        [TearDown]
        public override void TearDown()
        {
            _asset.Disable();
            Object.DestroyImmediate(_asset);
            base.TearDown();
        }

        [Test]
        public void Starts_Unknown()
        {
            using InputDeviceTracker tracker = new();

            Assert.AreEqual(InputDeviceKind.Unknown, tracker.Kind);
        }

        [Test]
        public void FollowsTheDeviceThatFiredAnAction()
        {
            using InputDeviceTracker tracker = new();

            Press(_keyboard.spaceKey);
            Release(_keyboard.spaceKey);
            Assert.AreEqual(InputDeviceKind.KeyboardMouse, tracker.Kind);

            Press(_gamepad.buttonSouth);
            Release(_gamepad.buttonSouth);
            Assert.AreEqual(InputDeviceKind.Gamepad, tracker.Kind);
        }

        [Test]
        public void Changed_FiresOnlyWhenTheKindChanges()
        {
            using InputDeviceTracker tracker = new();
            int raised = 0;
            tracker.Changed += _ => raised++;

            Press(_keyboard.spaceKey);
            Release(_keyboard.spaceKey);
            Press(_keyboard.spaceKey);
            Release(_keyboard.spaceKey);
            Press(_gamepad.buttonSouth);
            Release(_gamepad.buttonSouth);

            Assert.AreEqual(2, raised);
        }

        [Test]
        public void Dispose_StopsListening()
        {
            InputDeviceTracker tracker = new();
            tracker.Dispose();

            Press(_gamepad.buttonSouth);
            Release(_gamepad.buttonSouth);

            Assert.AreEqual(InputDeviceKind.Unknown, tracker.Kind);
        }

        [Test]
        public void Observe_ClassifiesDevicesAndIgnoresUnknownOnes()
        {
            using InputDeviceTracker tracker = new();

            tracker.Observe(_gamepad);
            Assert.AreEqual(InputDeviceKind.Gamepad, tracker.Kind);

            tracker.Observe(null);
            Assert.AreEqual(InputDeviceKind.Gamepad, tracker.Kind, "An unknown device must not reset the kind.");
        }

        [Test]
        public void KindOf_TreatsMouseAndKeyboardAsOne()
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>();

            Assert.AreEqual(InputDeviceKind.KeyboardMouse, InputDeviceTracker.KindOf(mouse));
            Assert.AreEqual(InputDeviceKind.KeyboardMouse, InputDeviceTracker.KindOf(_keyboard));
            Assert.AreEqual(InputDeviceKind.Gamepad, InputDeviceTracker.KindOf(_gamepad));
        }
    }
}
