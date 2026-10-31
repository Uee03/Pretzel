using System;
using System.Threading;
using NUnit.Framework;
using UnityEngine;

namespace Pretzel.Cloud.Tests
{
    public sealed class CloudRetryTests
    {
        private RecordingDelay _delay;

        [SetUp]
        public void SetUp() => _delay = new RecordingDelay();

        private CloudRetry Create(int attempts) => new(new RetryPolicy(attempts, TimeSpan.FromSeconds(1), 2d, TimeSpan.FromSeconds(30), 0d), _delay, () => 0.5d);

        private static async Awaitable<CloudResult<int>> FailThenSucceedAsync(int[] calls, int failures, CloudErrorKind kind)
        {
            await Awaitable.MainThreadAsync();
            calls[0]++;
            return calls[0] <= failures ? CloudResult<int>.Failure(kind, "fail " + calls[0]) : CloudResult<int>.Success(calls[0]);
        }

        [Test]
        public void SucceedsAfterTransientFailuresWithBackoff()
        {
            int[] calls = { 0 };
            CloudRetry retry = Create(4);
            CloudResult<int> result = Run.Result(retry.RunAsync(token => FailThenSucceedAsync(calls, 2, CloudErrorKind.Offline)));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(3));
            Assert.That(_delay.Delays, Is.EqualTo(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2) }));
        }

        [Test]
        public void GivesUpAfterMaxAttemptsAndReturnsTheLastError()
        {
            int[] calls = { 0 };
            CloudRetry retry = Create(3);
            CloudResult<int> result = Run.Result(retry.RunAsync(token => FailThenSucceedAsync(calls, 99, CloudErrorKind.ServiceUnavailable)));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error.Message, Is.EqualTo("fail 3"));
            Assert.That(calls[0], Is.EqualTo(3));
            Assert.That(_delay.Delays.Count, Is.EqualTo(2));
        }

        [Test]
        public void PermanentErrorsAreNotRetried()
        {
            int[] calls = { 0 };
            CloudRetry retry = Create(5);
            CloudResult<int> result = Run.Result(retry.RunAsync(token => FailThenSucceedAsync(calls, 99, CloudErrorKind.InvalidRequest)));
            Assert.That(result.Error.Kind, Is.EqualTo(CloudErrorKind.InvalidRequest));
            Assert.That(calls[0], Is.EqualTo(1));
            Assert.That(_delay.Delays, Is.Empty);
        }

        [Test]
        public void ValuelessOperationsRetryToo()
        {
            int calls = 0;
            CloudRetry retry = Create(3);
            CloudResult result = Run.Result(retry.RunAsync(async token =>
            {
                await Awaitable.MainThreadAsync();
                calls++;
                return calls < 2 ? CloudResult.Failure(CloudErrorKind.Timeout, "slow") : CloudResult.Success();
            }));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(calls, Is.EqualTo(2));
        }

        [Test]
        public void CancelledBeforeTheFirstAttemptThrowsAndNeverCallsTheOperation()
        {
            int[] calls = { 0 };
            using CancellationTokenSource source = new();
            source.Cancel();
            CloudRetry retry = Create(3);
            Assert.Throws<OperationCanceledException>(() => Run.Result(retry.RunAsync(token => FailThenSucceedAsync(calls, 0, CloudErrorKind.Offline), source.Token)));
            Assert.That(calls[0], Is.EqualTo(0));
        }

        [Test]
        public void CancellationDuringTheBackoffWaitStopsTheRetries()
        {
            int[] calls = { 0 };
            using CancellationTokenSource source = new();
            _delay.OnDelay = source.Cancel;
            CloudRetry retry = Create(5);
            Assert.Throws<OperationCanceledException>(() => Run.Result(retry.RunAsync(token => FailThenSucceedAsync(calls, 99, CloudErrorKind.Offline), source.Token)));
            Assert.That(calls[0], Is.EqualTo(1));
        }

        [Test]
        public void TheTokenIsPassedToTheOperation()
        {
            using CancellationTokenSource source = new();
            CancellationToken seen = default;
            CloudRetry retry = Create(1);
            Run.Result(retry.RunAsync(async token =>
            {
                await Awaitable.MainThreadAsync();
                seen = token;
                return CloudResult<int>.Success(1);
            }, source.Token));
            Assert.That(seen, Is.EqualTo(source.Token));
        }
    }
}
