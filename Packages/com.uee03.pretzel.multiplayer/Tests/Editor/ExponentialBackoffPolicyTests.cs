using System;
using NUnit.Framework;

namespace Pretzel.Multiplayer.Tests
{
    public sealed class ExponentialBackoffPolicyTests
    {
        [Test]
        public void Delays_DoubleUntilTheCap()
        {
            ExponentialBackoffPolicy policy = new ExponentialBackoffPolicy(6, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));

            double[] expected = { 1, 2, 4, 8, 10, 10 };
            for (int attempt = 1; attempt <= 6; attempt++)
            {
                Assert.AreEqual(expected[attempt - 1], policy.GetDelay(attempt).Value.TotalSeconds, 0.0001, "attempt " + attempt);
            }
        }

        [Test]
        public void AttemptsBeyondTheLimit_ReturnNull()
        {
            ExponentialBackoffPolicy policy = new ExponentialBackoffPolicy(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));

            Assert.IsNotNull(policy.GetDelay(3));
            Assert.IsNull(policy.GetDelay(4));
            Assert.IsNull(policy.GetDelay(0));
            Assert.IsNull(policy.GetDelay(-1));
        }

        [Test]
        public void ZeroAttempts_StopsImmediately()
        {
            ExponentialBackoffPolicy policy = new ExponentialBackoffPolicy(0, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));

            Assert.IsNull(policy.GetDelay(1));
        }

        [Test]
        public void Jitter_StaysWithinTheConfiguredFractionAndTheCap()
        {
            double next = 0d;
            ExponentialBackoffPolicy policy = new ExponentialBackoffPolicy(2, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), 2d, 0.5d, () => next);

            next = 0d;
            Assert.AreEqual(5d, policy.GetDelay(1).Value.TotalSeconds, 0.0001, "random 0 gives -jitter");
            next = 0.5d;
            Assert.AreEqual(10d, policy.GetDelay(1).Value.TotalSeconds, 0.0001, "random 0.5 gives no change");
            next = 0.999d;
            Assert.LessOrEqual(policy.GetDelay(1).Value.TotalSeconds, 12d, "never above the cap");
        }

        [Test]
        public void CustomFactor_IsApplied()
        {
            ExponentialBackoffPolicy policy = new ExponentialBackoffPolicy(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(100), 3d);

            Assert.AreEqual(9d, policy.GetDelay(3).Value.TotalSeconds, 0.0001);
        }

        [Test]
        public void InvalidArguments_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ExponentialBackoffPolicy(-1, TimeSpan.Zero, TimeSpan.Zero));
            Assert.Throws<ArgumentException>(() => new ExponentialBackoffPolicy(1, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ExponentialBackoffPolicy(1, TimeSpan.Zero, TimeSpan.Zero, 0.5d));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ExponentialBackoffPolicy(1, TimeSpan.Zero, TimeSpan.Zero, 2d, 1.5d));
        }

        [Test]
        public void SettingsPolicy_UsesTheConfiguredAttemptCount()
        {
            NetworkSettings settings = new NetworkSettings();

            IReconnectPolicy policy = settings.CreateReconnectPolicy();

            Assert.IsNotNull(policy.GetDelay(settings.ReconnectMaxAttempts));
            Assert.IsNull(policy.GetDelay(settings.ReconnectMaxAttempts + 1));
        }
    }
}
