using System;
using NUnit.Framework;

namespace Pretzel.Cloud.Tests
{
    public sealed class RetryPolicyTests
    {
        [Test]
        public void DelayGrowsExponentiallyAndIsCapped()
        {
            RetryPolicy policy = new(maxAttempts: 10, initialDelay: TimeSpan.FromSeconds(1), multiplier: 2d, maxDelay: TimeSpan.FromSeconds(5), jitterRatio: 0d);
            Assert.That(policy.GetDelay(1, null), Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(policy.GetDelay(2, null), Is.EqualTo(TimeSpan.FromSeconds(2)));
            Assert.That(policy.GetDelay(3, null), Is.EqualTo(TimeSpan.FromSeconds(4)));
            Assert.That(policy.GetDelay(4, null), Is.EqualTo(TimeSpan.FromSeconds(5)));
            Assert.That(policy.GetDelay(9, null), Is.EqualTo(TimeSpan.FromSeconds(5)));
        }

        [Test]
        public void JitterStaysWithinTheRatio()
        {
            RetryPolicy policy = new(maxAttempts: 5, initialDelay: TimeSpan.FromSeconds(10), multiplier: 1d, maxDelay: TimeSpan.FromSeconds(60), jitterRatio: 0.2d);
            Assert.That(policy.GetDelay(1, null, 0d).TotalSeconds, Is.EqualTo(8d).Within(0.0001d));
            Assert.That(policy.GetDelay(1, null, 0.5d).TotalSeconds, Is.EqualTo(10d).Within(0.0001d));
            Assert.That(policy.GetDelay(1, null, 1d).TotalSeconds, Is.EqualTo(12d).Within(0.0001d));
            Assert.That(policy.GetDelay(1, null, 7d).TotalSeconds, Is.EqualTo(12d).Within(0.0001d), "samples are clamped to 0..1");
        }

        [Test]
        public void ServerRetryAfterRaisesTheDelay()
        {
            RetryPolicy policy = new(maxAttempts: 5, initialDelay: TimeSpan.FromSeconds(1), maxDelay: TimeSpan.FromSeconds(30), jitterRatio: 0d);
            CloudError error = new(CloudErrorKind.RateLimited, "slow", 0, TimeSpan.FromSeconds(12));
            Assert.That(policy.GetDelay(1, error), Is.EqualTo(TimeSpan.FromSeconds(12)));
        }

        [Test]
        public void OnlyTransientErrorsRetryUntilAttemptsRunOut()
        {
            RetryPolicy policy = new(maxAttempts: 3);
            CloudError offline = new(CloudErrorKind.Offline, "x");
            Assert.That(policy.ShouldRetry(offline, 1), Is.True);
            Assert.That(policy.ShouldRetry(offline, 2), Is.True);
            Assert.That(policy.ShouldRetry(offline, 3), Is.False);
            Assert.That(policy.ShouldRetry(new CloudError(CloudErrorKind.InvalidRequest, "x"), 1), Is.False);
            Assert.That(policy.ShouldRetry(new CloudError(CloudErrorKind.Conflict, "x"), 1), Is.False);
            Assert.That(policy.ShouldRetry(new CloudError(CloudErrorKind.NotSignedIn, "x"), 1), Is.False);
        }

        [Test]
        public void RetryAfterBeyondMaxDelayIsNotWaitedFor()
        {
            RetryPolicy policy = new(maxAttempts: 5, maxDelay: TimeSpan.FromSeconds(10));
            CloudError longWait = new(CloudErrorKind.RateLimited, "x", 0, TimeSpan.FromMinutes(5));
            CloudError shortWait = new(CloudErrorKind.RateLimited, "x", 0, TimeSpan.FromSeconds(5));
            Assert.That(policy.ShouldRetry(longWait, 1), Is.False);
            Assert.That(policy.ShouldRetry(shortWait, 1), Is.True);
        }

        [Test]
        public void NoRetryMakesOneAttempt()
        {
            RetryPolicy policy = RetryPolicy.NoRetry();
            Assert.That(policy.MaxAttempts, Is.EqualTo(1));
            Assert.That(policy.ShouldRetry(new CloudError(CloudErrorKind.Offline, "x"), 1), Is.False);
        }

        [Test]
        public void InvalidSettingsAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RetryPolicy(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RetryPolicy(3, multiplier: 0.5d));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RetryPolicy(3, jitterRatio: 2d));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RetryPolicy(3, initialDelay: TimeSpan.FromSeconds(-1)));
            Assert.Throws<ArgumentNullException>(() => new RetryPolicy().ShouldRetry(null, 1));
        }
    }
}
