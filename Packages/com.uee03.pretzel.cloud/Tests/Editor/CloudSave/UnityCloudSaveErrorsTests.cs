using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Internal;
using Unity.Services.CloudSave.Models;
using Unity.Services.Core;
using UnityEngine;

namespace Pretzel.Cloud.CloudSave.Tests
{
    public sealed class UnityCloudSaveErrorsTests
    {
        [Test]
        public void ReasonsMapToKinds()
        {
            Assert.That(Map(Create<CloudSaveException>(CloudSaveExceptionReason.NoInternetConnection, 1, "m", null)), Is.EqualTo(CloudErrorKind.Offline));
            Assert.That(Map(Create<CloudSaveException>(CloudSaveExceptionReason.ProjectIdMissing, 0, "m", null)), Is.EqualTo(CloudErrorKind.NotInitialized));
            Assert.That(Map(Create<CloudSaveException>(CloudSaveExceptionReason.PlayerIdMissing, 0, "m", null)), Is.EqualTo(CloudErrorKind.NotSignedIn));
            Assert.That(Map(Create<CloudSaveException>(CloudSaveExceptionReason.AccessTokenMissing, 0, "m", null)), Is.EqualTo(CloudErrorKind.NotSignedIn));
            Assert.That(Map(Create<CloudSaveException>(CloudSaveExceptionReason.InvalidArgument, 0, "m", null)), Is.EqualTo(CloudErrorKind.InvalidRequest));
            Assert.That(Map(Create<CloudSaveException>(CloudSaveExceptionReason.KeyLimitExceeded, 0, "m", null)), Is.EqualTo(CloudErrorKind.InvalidRequest));
            Assert.That(Map(Create<CloudSaveException>(CloudSaveExceptionReason.Unauthorized, 0, "m", null)), Is.EqualTo(CloudErrorKind.Unauthorized));
            Assert.That(Map(Create<CloudSaveException>(CloudSaveExceptionReason.NotFound, 0, "m", null)), Is.EqualTo(CloudErrorKind.NotFound));
            Assert.That(Map(Create<CloudSaveException>(CloudSaveExceptionReason.TooManyRequests, 0, "m", null)), Is.EqualTo(CloudErrorKind.RateLimited));
            Assert.That(Map(Create<CloudSaveException>(CloudSaveExceptionReason.ServiceUnavailable, 0, "m", null)), Is.EqualTo(CloudErrorKind.ServiceUnavailable));
            Assert.That(Map(Create<CloudSaveException>(CloudSaveExceptionReason.Conflict, 0, "m", null)), Is.EqualTo(CloudErrorKind.Conflict));
        }

        [Test]
        public void UnknownReasonFallsBackToTheErrorCode()
        {
            CloudSaveException exception = Create<CloudSaveException>(CloudSaveExceptionReason.Unknown, CommonErrorCodes.Timeout, "m", null);
            Assert.That(Map(exception), Is.EqualTo(CloudErrorKind.Timeout));
        }

        [Test]
        public void SubclassesMapBeforeTheBaseReason()
        {
            Assert.That(Map(Create<CloudSaveConflictException>(CloudSaveExceptionReason.Unknown, 0, "m", null, null)), Is.EqualTo(CloudErrorKind.Conflict));
            Assert.That(Map(Create<CloudSaveValidationException>(CloudSaveExceptionReason.Unknown, 0, "m", null)), Is.EqualTo(CloudErrorKind.InvalidRequest));
        }

        [Test]
        public void RateLimitingKeepsTheRetryAfterWait()
        {
            CloudSaveRateLimitedException exception = Create<CloudSaveRateLimitedException>(CloudSaveExceptionReason.TooManyRequests, 429, "slow down", 7f, null);
            CloudError error = UnityCloudSaveErrors.Map(exception);
            Assert.That(error.Kind, Is.EqualTo(CloudErrorKind.RateLimited));
            Assert.That(error.RetryAfter, Is.EqualTo(TimeSpan.FromSeconds(7)));
            Assert.That(error.Code, Is.EqualTo(429));
            Assert.That(error.IsTransient, Is.True);
        }

        [Test]
        public void OtherExceptionsUseTheSharedMapping()
        {
            Assert.That(UnityCloudSaveErrors.Map(new RequestFailedException(CommonErrorCodes.ServiceUnavailable, "m")).Kind, Is.EqualTo(CloudErrorKind.ServiceUnavailable));
            Assert.That(UnityCloudSaveErrors.Map(new ServicesInitializationException("m")).Kind, Is.EqualTo(CloudErrorKind.NotInitialized));
            Assert.That(UnityCloudSaveErrors.Map(new TimeoutException()).Kind, Is.EqualTo(CloudErrorKind.Timeout));
        }

        private static CloudErrorKind Map(Exception exception) => UnityCloudSaveErrors.Map(exception).Kind;

        // The SDK exceptions have internal constructors, so the tests build them by reflection.
        private static T Create<T>(params object[] arguments) where T : Exception
        {
            return (T)Activator.CreateInstance(typeof(T), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, arguments, null);
        }
    }
}
