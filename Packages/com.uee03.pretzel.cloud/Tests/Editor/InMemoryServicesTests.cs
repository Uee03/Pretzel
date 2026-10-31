using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pretzel.Cloud.InMemory;
using Pretzel.Data;

namespace Pretzel.Cloud.Tests
{
    public sealed class InMemoryServicesTests
    {
        [Serializable]
        private sealed class AddRequest
        {
            public int a;
            public int b;
        }

        [Serializable]
        private sealed class AddResponse
        {
            public int sum;
        }

        [Serializable]
        private sealed class Tuning
        {
            public float speed;
            public string mode;
        }

        [Test]
        public void AuthRequiresInitializeThenSignsInAndRaisesStates()
        {
            InMemoryCloudAuthService auth = new("player-1");
            List<CloudAuthState> states = new();
            auth.StateChanged += states.Add;

            Assert.That(Run.Result(auth.SignInAnonymouslyAsync()).Error.Kind, Is.EqualTo(CloudErrorKind.NotInitialized));
            Assert.That(Run.Result(auth.InitializeAsync()).IsSuccess, Is.True);
            Assert.That(Run.Result(auth.SignInAnonymouslyAsync()).IsSuccess, Is.True);
            Assert.That(auth.IsSignedIn, Is.True);
            Assert.That(auth.PlayerId, Is.EqualTo("player-1"));
            Assert.That(states, Is.EqualTo(new[] { CloudAuthState.SigningIn, CloudAuthState.SignedIn }));

            Assert.That(Run.Result(auth.SignInAnonymouslyAsync()).IsSuccess, Is.True, "already signed in");
            Assert.That(states.Count, Is.EqualTo(2));

            auth.SignOut();
            Assert.That(auth.IsSignedIn, Is.False);
            Assert.That(auth.PlayerId, Is.Null);
            Assert.That(states[states.Count - 1], Is.EqualTo(CloudAuthState.SignedOut));
        }

        [Test]
        public void LinkingNeedsASignedInPlayer()
        {
            InMemoryCloudAuthService auth = new();
            CloudIdentity identity = new(CloudIdentityProvider.Google, "token");
            Run.Result(auth.InitializeAsync());
            Assert.That(Run.Result(auth.LinkIdentityAsync(identity)).Error.Kind, Is.EqualTo(CloudErrorKind.NotSignedIn));
            Run.Result(auth.SignInWithIdentityAsync(identity));
            Assert.That(Run.Result(auth.LinkIdentityAsync(new CloudIdentity(CloudIdentityProvider.Apple, "t"))).IsSuccess, Is.True);
            Assert.That(Run.Result(auth.AddUsernamePasswordAsync("name", "Pass-word1")).IsSuccess, Is.True);
        }

        [Test]
        public void ExpiringTheSessionIsReported()
        {
            InMemoryCloudAuthService auth = new();
            Run.Result(auth.InitializeAsync());
            Run.Result(auth.SignInAnonymouslyAsync());
            auth.Expire();
            Assert.That(auth.State, Is.EqualTo(CloudAuthState.Expired));
            Assert.That(auth.IsSignedIn, Is.False);
        }

        [Test]
        public void IdentityNeverPrintsItsToken()
        {
            CloudIdentity identity = new(CloudIdentityProvider.Steam, "secret-ticket", "identity");
            Assert.That(identity.ToString(), Does.Not.Contain("secret"));
            Assert.Throws<ArgumentException>(() => new CloudIdentity(CloudIdentityProvider.Steam, ""));
        }

        [Test]
        public void CloudCodeRoundTripsTypedRequestsThroughTheSerializer()
        {
            InMemoryCloudCodeService code = new(new NewtonsoftSerializer());
            code.RegisterModule<AddRequest, AddResponse>("Math", "Add", request => new AddResponse { sum = request.a + request.b });

            CloudResult<AddResponse> result = Run.Result(code.CallModuleAsync<AddRequest, AddResponse>("Math", "Add", new AddRequest { a = 2, b = 3 }));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.sum, Is.EqualTo(5));
            Assert.That(code.LastRequestJson, Does.Contain("\"a\":2"));
        }

        [Test]
        public void CloudCodeScriptsAndMissingEndpoints()
        {
            InMemoryCloudCodeService code = new(new NewtonsoftSerializer());
            code.RegisterScript<AddRequest, AddResponse>("add", request => new AddResponse { sum = request.a + request.b });

            Assert.That(Run.Result(code.CallScriptAsync<AddRequest, AddResponse>("add", new AddRequest { a = 1, b = 1 })).Value.sum, Is.EqualTo(2));
            Assert.That(Run.Result(code.CallScriptAsync<AddRequest, AddResponse>("other", null)).Error.Kind, Is.EqualTo(CloudErrorKind.NotFound));
            Assert.That(Run.Result(code.CallModuleAsync<AddRequest, AddResponse>("Math", "Add", null)).Error.Kind, Is.EqualTo(CloudErrorKind.NotFound));
        }

        [Test]
        public void CloudCodeResponseOfTheWrongShapeIsASerializationError()
        {
            InMemoryCloudCodeService code = new(new NewtonsoftSerializer());
            code.RegisterModule<AddRequest, string>("Math", "Name", request => "not an object");
            CloudResult<AddResponse> result = Run.Result(code.CallModuleAsync<AddRequest, AddResponse>("Math", "Name", new AddRequest()));
            Assert.That(result.Error.Kind, Is.EqualTo(CloudErrorKind.Serialization));
        }

        [Test]
        public void RemoteConfigGettersFallBackToDefaultsBeforeAndAfterAFetch()
        {
            TestClock clock = new();
            InMemoryRemoteConfigService config = new(new NewtonsoftSerializer(), clock.Read);
            config.SetServerValue("enabled", true);
            config.SetServerValue("count", 4L);
            config.SetServerValue("ratio", 0.5d);
            config.SetServerValue("title", "Hello");
            config.SetServerValue("tuning", "{\"speed\":2.5,\"mode\":\"fast\"}");

            Assert.That(config.GetBool("enabled", false), Is.False, "nothing is visible before a fetch");
            Assert.That(config.LastFetch.Origin, Is.EqualTo(RemoteConfigOrigin.Default));

            RemoteConfigFetchInfo info = Run.Result(config.FetchAsync()).Value;
            Assert.That(info.IsFresh, Is.True);
            Assert.That(info.KeyCount, Is.EqualTo(5));
            Assert.That(info.FetchedAtUtc, Is.EqualTo(clock.Now));
            Assert.That(config.GetBool("enabled"), Is.True);
            Assert.That(config.GetInt("count"), Is.EqualTo(4));
            Assert.That(config.GetLong("count"), Is.EqualTo(4L));
            Assert.That(config.GetFloat("ratio"), Is.EqualTo(0.5f));
            Assert.That(config.GetString("title"), Is.EqualTo("Hello"));
            Assert.That(config.HasKey("title"), Is.True);
            Assert.That(config.Keys.Count, Is.EqualTo(5));

            Tuning tuning = config.GetJson<Tuning>("tuning");
            Assert.That(tuning.speed, Is.EqualTo(2.5f));
            Assert.That(tuning.mode, Is.EqualTo("fast"));

            Assert.That(config.GetInt("missing", 7), Is.EqualTo(7));
            Assert.That(config.GetBool("title", true), Is.True, "wrong type falls back");
            Assert.That(config.GetJson<Tuning>("title"), Is.Null, "malformed JSON falls back");
            Assert.That(config.GetString("missing", "x"), Is.EqualTo("x"));
        }

        [Test]
        public void RemoteConfigKeepsCachedValuesWhenTheFetchGoesOffline()
        {
            InMemoryRemoteConfigService config = new(new NewtonsoftSerializer());
            config.SetServerValue("title", "v1");
            Run.Result(config.FetchAsync());
            config.SetServerValue("title", "v2");
            config.Faults.Offline = true;

            CloudResult<RemoteConfigFetchInfo> result = Run.Result(config.FetchAsync());
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Origin, Is.EqualTo(RemoteConfigOrigin.Cached));
            Assert.That(result.Value.IsFresh, Is.False);
            Assert.That(config.GetString("title"), Is.EqualTo("v1"));
        }

        [Test]
        public void RemoteConfigReportsOtherFailures()
        {
            InMemoryRemoteConfigService config = new(new NewtonsoftSerializer());
            config.Faults.Enqueue(CloudErrorKind.Unauthorized);
            Assert.That(Run.Result(config.FetchAsync()).Error.Kind, Is.EqualTo(CloudErrorKind.Unauthorized));
        }
    }
}
