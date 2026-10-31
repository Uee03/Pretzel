using System;
using NUnit.Framework;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace Pretzel.Cloud.Authentication.Tests
{
    public sealed class UnityAuthenticationErrorsTests
    {
        [Test]
        public void AuthenticationCodesMapToKinds()
        {
            Assert.That(Map(AuthenticationErrorCodes.ClientNoActiveSession), Is.EqualTo(CloudErrorKind.NotSignedIn));
            Assert.That(Map(AuthenticationErrorCodes.InvalidSessionToken), Is.EqualTo(CloudErrorKind.Unauthorized));
            Assert.That(Map(AuthenticationErrorCodes.BannedUser), Is.EqualTo(CloudErrorKind.Unauthorized));
            Assert.That(Map(AuthenticationErrorCodes.AccountAlreadyLinked), Is.EqualTo(CloudErrorKind.Conflict));
            Assert.That(Map(AuthenticationErrorCodes.ClientUnlinkExternalIdNotFound), Is.EqualTo(CloudErrorKind.NotFound));
            Assert.That(Map(AuthenticationErrorCodes.InvalidParameters), Is.EqualTo(CloudErrorKind.InvalidRequest));
            Assert.That(Map(AuthenticationErrorCodes.ClientInvalidUserState), Is.EqualTo(CloudErrorKind.InvalidRequest));
            Assert.That(Map(AuthenticationErrorCodes.AccountLinkLimitExceeded), Is.EqualTo(CloudErrorKind.InvalidRequest));
        }

        [Test]
        public void CommonErrorCodesFallThroughToTheSharedTable()
        {
            Assert.That(MapFailure(CommonErrorCodes.TransportError), Is.EqualTo(CloudErrorKind.Offline));
            Assert.That(MapFailure(CommonErrorCodes.TooManyRequests), Is.EqualTo(CloudErrorKind.RateLimited));
            Assert.That(MapFailure(CommonErrorCodes.Timeout), Is.EqualTo(CloudErrorKind.Timeout));
            Assert.That(MapFailure(CommonErrorCodes.InvalidToken), Is.EqualTo(CloudErrorKind.Unauthorized));
        }

        [Test]
        public void ServicesNotInitializedAndPlainExceptionsAreMapped()
        {
            Assert.That(UnityAuthenticationErrors.Map(new ServicesInitializationException("not initialized")).Kind, Is.EqualTo(CloudErrorKind.NotInitialized));
            Assert.That(UnityAuthenticationErrors.Map(new TimeoutException()).Kind, Is.EqualTo(CloudErrorKind.Timeout));
        }

        [Test]
        public void TheCodeAndMessageAreKept()
        {
            CloudError error = UnityAuthenticationErrors.Map(AuthenticationException.Create(AuthenticationErrorCodes.AccountAlreadyLinked, "linked elsewhere"));
            Assert.That(error.Code, Is.EqualTo(AuthenticationErrorCodes.AccountAlreadyLinked));
            Assert.That(error.Message, Is.EqualTo("linked elsewhere"));
        }

        [Test]
        public void TheServiceStartsSignedOutAndRefusesSignInBeforeInitialize()
        {
            using UnityAuthenticationService service = new();
            Assert.That(service.State, Is.EqualTo(CloudAuthState.SignedOut));
            Assert.That(service.IsSignedIn, Is.False);
            Assert.That(service.PlayerId, Is.Null);
            Assert.That(Result(service.SignInAnonymouslyAsync()).Error.Kind, Is.EqualTo(CloudErrorKind.NotInitialized));
            service.SignOut();
        }

        [Test]
        public void IdentitiesThatNeedASecondValueAreRejectedBeforeTheSdkIsCalled()
        {
            using UnityAuthenticationService service = new();
            CloudResult result = Result(service.SignInWithIdentityAsync(new CloudIdentity(CloudIdentityProvider.Steam, "ticket")));
            Assert.That(result.Error.Kind, Is.EqualTo(CloudErrorKind.InvalidRequest));
        }

        private static CloudErrorKind Map(int code) => UnityAuthenticationErrors.Map(AuthenticationException.Create(code, "m")).Kind;

        private static CloudErrorKind MapFailure(int code) => UnityAuthenticationErrors.Map(new RequestFailedException(code, "m")).Kind;

        // These calls fail before any await, so the awaitable is already complete.
        private static CloudResult Result(Awaitable<CloudResult> awaitable)
        {
            Awaitable<CloudResult>.Awaiter awaiter = awaitable.GetAwaiter();
            Assert.That(awaiter.IsCompleted, Is.True);
            return awaiter.GetResult();
        }
    }
}
