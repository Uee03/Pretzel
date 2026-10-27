using System;
using NUnit.Framework;

namespace Pretzel.Api.Tests
{
    public sealed class ApiClientTests
    {
        [TestCase("https://api.example.com", "users", "https://api.example.com/users")]
        [TestCase("https://api.example.com/", "/users", "https://api.example.com/users")]
        [TestCase("https://api.example.com/v1", "users/1", "https://api.example.com/v1/users/1")]
        [TestCase(null, "https://other.example.com/x", "https://other.example.com/x")]
        public void ResolvesRelativeAndAbsoluteUrls(string baseUrl, string url, string expected)
        {
            Assert.That(new ApiClient(baseUrl).ResolveUrl(url), Is.EqualTo(expected));
        }

        [Test]
        public void RelativeUrlWithoutBaseThrows()
        {
            Assert.Throws<ArgumentException>(() => new ApiClient().ResolveUrl("users"));
            Assert.Throws<ArgumentException>(() => new ApiClient("https://x").ResolveUrl(""));
        }

        [Test]
        public void CallOptionsOverrideDefaultsAndMergeHeaders()
        {
            ApiClient client = new("https://x");
            client.Defaults.SetHeader("X-Client", "pretzel").SetBearerToken("default");
            RequestOptions call = new RequestOptions { Retries = 3 }.SetBearerToken("call");

            RequestOptions merged = client.Merge(call);
            Assert.That(merged.Retries, Is.EqualTo(3));
            Assert.That(merged.TimeoutSeconds, Is.EqualTo(30f));
            Assert.That(merged.ContentType, Is.EqualTo("application/json"));
            Assert.That(merged.Headers["x-client"], Is.EqualTo("pretzel"), "Header names are case-insensitive.");
            Assert.That(merged.Headers["Authorization"], Is.EqualTo("Bearer call"));
        }

        [Test]
        public void MergeWithoutCallOptionsUsesDefaults()
        {
            ApiClient client = new("https://x");
            RequestOptions merged = client.Merge(null);
            Assert.That(merged.Retries, Is.EqualTo(0));
            Assert.That(merged.RetryDelaySeconds, Is.EqualTo(0.5f));
            Assert.That(merged.Progress, Is.Null);
        }
    }
}
