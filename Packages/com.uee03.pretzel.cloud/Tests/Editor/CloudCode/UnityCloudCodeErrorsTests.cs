using System;
using System.Reflection;
using NUnit.Framework;
using Unity.Services.CloudCode;
using Unity.Services.Core;

namespace Pretzel.Cloud.CloudCode.Tests
{
    public sealed class UnityCloudCodeErrorsTests
    {
        [Test]
        public void ReasonsMapToKinds()
        {
            Assert.That(Map(Create<CloudCodeException>(CloudCodeExceptionReason.NoInternetConnection, 1, "m", null)), Is.EqualTo(CloudErrorKind.Offline));
            Assert.That(Map(Create<CloudCodeException>(CloudCodeExceptionReason.ProjectIdMissing, 0, "m", null)), Is.EqualTo(CloudErrorKind.NotInitialized));
            Assert.That(Map(Create<CloudCodeException>(CloudCodeExceptionReason.PlayerIdMissing, 0, "m", null)), Is.EqualTo(CloudErrorKind.NotSignedIn));
            Assert.That(Map(Create<CloudCodeException>(CloudCodeExceptionReason.AccessTokenMissing, 0, "m", null)), Is.EqualTo(CloudErrorKind.NotSignedIn));
            Assert.That(Map(Create<CloudCodeException>(CloudCodeExceptionReason.InvalidArgument, 0, "m", null)), Is.EqualTo(CloudErrorKind.InvalidRequest));
            Assert.That(Map(Create<CloudCodeException>(CloudCodeExceptionReason.Unauthorized, 0, "m", null)), Is.EqualTo(CloudErrorKind.Unauthorized));
            Assert.That(Map(Create<CloudCodeException>(CloudCodeExceptionReason.NotFound, 0, "m", null)), Is.EqualTo(CloudErrorKind.NotFound));
            Assert.That(Map(Create<CloudCodeException>(CloudCodeExceptionReason.TooManyRequests, 0, "m", null)), Is.EqualTo(CloudErrorKind.RateLimited));
            Assert.That(Map(Create<CloudCodeException>(CloudCodeExceptionReason.ServiceUnavailable, 0, "m", null)), Is.EqualTo(CloudErrorKind.ServiceUnavailable));
            Assert.That(Map(Create<CloudCodeException>(CloudCodeExceptionReason.ScriptError, 0, "m", null)), Is.EqualTo(CloudErrorKind.ScriptError));
        }

        [Test]
        public void ScriptErrorsAreNotTransient()
        {
            CloudError error = UnityCloudCodeErrors.Map(Create<CloudCodeException>(CloudCodeExceptionReason.ScriptError, 9009, "the module threw", null));
            Assert.That(error.IsTransient, Is.False);
            Assert.That(error.Code, Is.EqualTo(9009));
        }

        [Test]
        public void RateLimitingKeepsTheRetryAfterWait()
        {
            CloudCodeRateLimitedException exception = Create<CloudCodeRateLimitedException>(CloudCodeExceptionReason.TooManyRequests, 429, "slow down", null, 9);
            CloudError error = UnityCloudCodeErrors.Map(exception);
            Assert.That(error.Kind, Is.EqualTo(CloudErrorKind.RateLimited));
            Assert.That(error.RetryAfter, Is.EqualTo(TimeSpan.FromSeconds(9)));
        }

        [Test]
        public void OtherExceptionsUseTheSharedMapping()
        {
            Assert.That(UnityCloudCodeErrors.Map(new RequestFailedException(CommonErrorCodes.TransportError, "m")).Kind, Is.EqualTo(CloudErrorKind.Offline));
            Assert.That(UnityCloudCodeErrors.Map(new ServicesInitializationException("m")).Kind, Is.EqualTo(CloudErrorKind.NotInitialized));
            Assert.That(UnityCloudCodeErrors.Map(new FormatException("bad json")).Kind, Is.EqualTo(CloudErrorKind.Serialization));
            Assert.That(UnityCloudCodeErrors.Map(new TimeoutException()).Kind, Is.EqualTo(CloudErrorKind.Timeout));
        }

        [Test]
        public void TheBuilderExtensionAddsTheService()
        {
            CloudServices services = CloudServices.CreateBuilder().UseUnityCloudCode().Build();
            Assert.That(services.Code, Is.TypeOf<Pretzel.Cloud.CloudCode.UnityCloudCodeService>());
        }

        private static CloudErrorKind Map(Exception exception) => UnityCloudCodeErrors.Map(exception).Kind;

        // The SDK exceptions have internal constructors, so the tests build them by reflection.
        private static T Create<T>(params object[] arguments) where T : Exception
        {
            return (T)Activator.CreateInstance(typeof(T), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, arguments, null);
        }
    }
}
