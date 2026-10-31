using System;
using NUnit.Framework;

namespace Pretzel.Cloud.Tests
{
    public sealed class CloudErrorMapperTests
    {
        [TestCase(1, CloudErrorKind.Offline)]
        [TestCase(2, CloudErrorKind.Timeout)]
        [TestCase(3, CloudErrorKind.ServiceUnavailable)]
        [TestCase(5, CloudErrorKind.InvalidRequest)]
        [TestCase(50, CloudErrorKind.RateLimited)]
        [TestCase(51, CloudErrorKind.Unauthorized)]
        [TestCase(52, CloudErrorKind.Unauthorized)]
        [TestCase(53, CloudErrorKind.Unauthorized)]
        [TestCase(54, CloudErrorKind.NotFound)]
        [TestCase(55, CloudErrorKind.InvalidRequest)]
        [TestCase(56, CloudErrorKind.Unauthorized)]
        [TestCase(57, CloudErrorKind.Unauthorized)]
        [TestCase(58, CloudErrorKind.Conflict)]
        [TestCase(0, CloudErrorKind.Unknown)]
        [TestCase(4, CloudErrorKind.Unknown)]
        [TestCase(12345, CloudErrorKind.Unknown)]
        public void ServiceErrorCodesMapToKinds(int code, CloudErrorKind expected)
        {
            CloudError error = CloudErrorMapper.FromServiceErrorCode(code, "message");
            Assert.That(error.Kind, Is.EqualTo(expected));
            Assert.That(error.Code, Is.EqualTo(code));
            Assert.That(error.Message, Is.EqualTo("message"));
        }

        [TestCase(0, CloudErrorKind.Offline)]
        [TestCase(401, CloudErrorKind.Unauthorized)]
        [TestCase(403, CloudErrorKind.Unauthorized)]
        [TestCase(404, CloudErrorKind.NotFound)]
        [TestCase(408, CloudErrorKind.Timeout)]
        [TestCase(409, CloudErrorKind.Conflict)]
        [TestCase(412, CloudErrorKind.Conflict)]
        [TestCase(429, CloudErrorKind.RateLimited)]
        [TestCase(400, CloudErrorKind.InvalidRequest)]
        [TestCase(422, CloudErrorKind.InvalidRequest)]
        [TestCase(500, CloudErrorKind.ServiceUnavailable)]
        [TestCase(503, CloudErrorKind.ServiceUnavailable)]
        [TestCase(504, CloudErrorKind.Timeout)]
        [TestCase(200, CloudErrorKind.Unknown)]
        public void HttpStatusesMapToKinds(int status, CloudErrorKind expected)
        {
            Assert.That(CloudErrorMapper.FromHttpStatus(status, "m").Kind, Is.EqualTo(expected));
        }

        [Test]
        public void RetryAfterIsKept()
        {
            CloudError error = CloudErrorMapper.FromHttpStatus(429, "slow down", TimeSpan.FromSeconds(7));
            Assert.That(error.RetryAfter, Is.EqualTo(TimeSpan.FromSeconds(7)));
        }

        [Test]
        public void PlainExceptionsMapToKinds()
        {
            Assert.That(CloudErrorMapper.FromException(new TimeoutException()).Kind, Is.EqualTo(CloudErrorKind.Timeout));
            Assert.That(CloudErrorMapper.FromException(new System.Net.Http.HttpRequestException("x")).Kind, Is.EqualTo(CloudErrorKind.Offline));
            Assert.That(CloudErrorMapper.FromException(new System.Net.Sockets.SocketException()).Kind, Is.EqualTo(CloudErrorKind.Offline));
            Assert.That(CloudErrorMapper.FromException(new ArgumentException("x")).Kind, Is.EqualTo(CloudErrorKind.InvalidRequest));
            Assert.That(CloudErrorMapper.FromException(new FormatException("x")).Kind, Is.EqualTo(CloudErrorKind.Serialization));
            Assert.That(CloudErrorMapper.FromException(new InvalidOperationException("x")).Kind, Is.EqualTo(CloudErrorKind.Unknown));
        }

        [Test]
        public void NullExceptionIsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => CloudErrorMapper.FromException(null));
        }

        [TestCase(CloudErrorKind.Offline, true, false)]
        [TestCase(CloudErrorKind.Timeout, true, false)]
        [TestCase(CloudErrorKind.RateLimited, true, false)]
        [TestCase(CloudErrorKind.ServiceUnavailable, true, false)]
        [TestCase(CloudErrorKind.NotSignedIn, false, true)]
        [TestCase(CloudErrorKind.Unauthorized, false, true)]
        [TestCase(CloudErrorKind.NotInitialized, false, true)]
        [TestCase(CloudErrorKind.Conflict, false, false)]
        [TestCase(CloudErrorKind.InvalidRequest, false, false)]
        [TestCase(CloudErrorKind.ScriptError, false, false)]
        [TestCase(CloudErrorKind.Unknown, false, false)]
        public void KindsClassifyAsTransientOrNeedingSignIn(CloudErrorKind kind, bool transient, bool needsSignIn)
        {
            CloudError error = new(kind, "m");
            Assert.That(error.IsTransient, Is.EqualTo(transient));
            Assert.That(error.NeedsSignIn, Is.EqualTo(needsSignIn));
        }

        [Test]
        public void ResultsCarryValueOrError()
        {
            CloudResult<int> ok = CloudResult<int>.Success(5);
            CloudResult<int> failed = CloudResult<int>.Failure(CloudErrorKind.NotFound, "gone");
            Assert.That(ok.IsSuccess, Is.True);
            Assert.That(ok.ValueOr(9), Is.EqualTo(5));
            Assert.That(failed.IsSuccess, Is.False);
            Assert.That(failed.Value, Is.EqualTo(0));
            Assert.That(failed.ValueOr(9), Is.EqualTo(9));
            Assert.That(failed.TryGetValue(out int value), Is.False);
            Assert.That(value, Is.EqualTo(0));
            Assert.That(failed.ToResult().Error.Kind, Is.EqualTo(CloudErrorKind.NotFound));
            Assert.That(ok.ToResult().IsSuccess, Is.True);
            Assert.That(CloudResult.Success().IsSuccess, Is.True);
            Assert.That(CloudResult.Failure(CloudErrorKind.Offline, "x").Error.Kind, Is.EqualTo(CloudErrorKind.Offline));
            Assert.Throws<ArgumentNullException>(() => CloudResult.Failure(null));
        }

        [Test]
        public void SerializationFailuresAreRecognized()
        {
            Assert.That(CloudSerialization.IsFailure(new FormatException()), Is.True);
            Assert.That(CloudSerialization.IsFailure(new ArgumentException()), Is.True);
            Assert.That(CloudSerialization.IsFailure(new OperationCanceledException()), Is.False);
            Assert.That(CloudSerialization.IsFailure(new NullReferenceException()), Is.False);
            Assert.That(CloudSerialization.IsFailure(null), Is.False);
            Exception newtonsoft = Assert.Catch(() => Newtonsoft.Json.JsonConvert.DeserializeObject<int[]>("{ not json"));
            Assert.That(CloudSerialization.IsFailure(newtonsoft), Is.True);
        }
    }
}
