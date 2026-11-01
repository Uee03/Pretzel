using NUnit.Framework;

namespace Pretzel.Sfx.Tests
{
    public sealed class SfxThrottleTests
    {
        [Test]
        public void FirstPlay_IsAlwaysAllowed()
        {
            SfxThrottle throttle = new();

            Assert.IsTrue(throttle.TryAcquire("hit", 10f, 5f));
        }

        [Test]
        public void ASecondPlayInsideTheCooldown_IsRefused()
        {
            SfxThrottle throttle = new();
            throttle.TryAcquire("hit", 10f, 0.5f);

            Assert.IsFalse(throttle.TryAcquire("hit", 10.2f, 0.5f));
        }

        [Test]
        public void APlayAfterTheCooldown_IsAllowed()
        {
            SfxThrottle throttle = new();
            throttle.TryAcquire("hit", 10f, 0.5f);

            Assert.IsTrue(throttle.TryAcquire("hit", 10.5f, 0.5f));
        }

        [Test]
        public void ARefusal_DoesNotRestartTheCooldown()
        {
            SfxThrottle throttle = new();
            throttle.TryAcquire("hit", 10f, 0.5f);
            throttle.TryAcquire("hit", 10.4f, 0.5f);

            Assert.IsTrue(throttle.TryAcquire("hit", 10.5f, 0.5f));
        }

        [Test]
        public void ZeroCooldown_AllowsEveryCall()
        {
            SfxThrottle throttle = new();

            for (int i = 0; i < 10; i++)
            {
                Assert.IsTrue(throttle.TryAcquire("hit", 1f, 0f));
            }
        }

        [Test]
        public void SoundsAreThrottledIndependently()
        {
            SfxThrottle throttle = new();
            throttle.TryAcquire("hit", 1f, 5f);

            Assert.IsTrue(throttle.TryAcquire("coin", 1f, 5f));
        }

        [Test]
        public void IdsAreCaseSensitive()
        {
            SfxThrottle throttle = new();
            throttle.TryAcquire("Hit", 1f, 5f);

            Assert.IsTrue(throttle.TryAcquire("hit", 1f, 5f));
        }

        [Test]
        public void Reset_ForgetsEverySound()
        {
            SfxThrottle throttle = new();
            throttle.TryAcquire("hit", 1f, 5f);

            throttle.Reset();

            Assert.IsTrue(throttle.TryAcquire("hit", 1f, 5f));
        }
    }
}
