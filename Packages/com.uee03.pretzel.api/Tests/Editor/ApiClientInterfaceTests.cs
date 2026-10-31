using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Pretzel.Api.Tests
{
    public sealed class ApiClientInterfaceTests
    {
        [Test]
        public void ApiClientIsUsableThroughTheInterface()
        {
            IApiClient client = new ApiClient("https://api.example.com");
            Assert.That(client.BaseUrl, Is.EqualTo("https://api.example.com"));
            client.BaseUrl = "https://other.example.com";
            Assert.That(client.BaseUrl, Is.EqualTo("https://other.example.com"));
            Assert.That(client.Defaults.TimeoutSeconds, Is.EqualTo(30f));
        }

        [Test]
        public void ResponseFactoriesBuildResultsForFakes()
        {
            Response<int> ok = Response<int>.Success(7);
            Assert.That(ok.Ok, Is.True);
            Assert.That(ok.StatusCode, Is.EqualTo(200));
            Assert.That(ok.Data, Is.EqualTo(7));
            Assert.That(ok.Headers, Is.Empty);

            Response<int> failed = Response<int>.Failure("nope", 503, "body", true);
            Assert.That(failed.Ok, Is.False);
            Assert.That(failed.StatusCode, Is.EqualTo(503));
            Assert.That(failed.Error, Is.EqualTo("nope"));
            Assert.That(failed.ErrorBody, Is.EqualTo("body"));
            Assert.That(failed.TimedOut, Is.True);
        }

        [Test]
        public async Task ServiceUnderTestRunsAgainstAFakeClientAsync()
        {
            FakeApiClient fake = new();
            TodoTitles service = new(fake);

            string title = await service.LoadTitleAsync(5);

            Assert.That(title, Is.EqualTo("buy milk"));
            Assert.That(fake.RequestedUrls, Is.EqualTo(new[] { "todos/5" }));
        }

        [Test]
        public async Task ServiceReportsAFailedResponseAsync()
        {
            FakeApiClient fake = new() { Fail = true };
            Assert.That(await new TodoTitles(fake).LoadTitleAsync(5), Is.EqualTo("unavailable"));
        }

        // The consumer takes the interface, so the test needs no network and no UnityWebRequest.
        private sealed class TodoTitles
        {
            private readonly IApiClient _client;

            public TodoTitles(IApiClient client)
            {
                _client = client;
            }

            public async Awaitable<string> LoadTitleAsync(int id)
            {
                Response<Todo> response = await _client.GetJsonAsync<Todo>("todos/" + id);
                return response.Ok ? response.Data.title : "unavailable";
            }
        }

        [Serializable]
        private sealed class Todo
        {
            public string title;
        }

        private sealed class FakeApiClient : IApiClient
        {
            private readonly List<string> _requestedUrls = new();

            public bool Fail { get; set; }

            public IReadOnlyList<string> RequestedUrls => _requestedUrls;

            public string BaseUrl { get; set; }

            public RequestOptions Defaults { get; } = new();

            public Awaitable<Response<T>> GetJsonAsync<T>(string url, RequestOptions options = null, CancellationToken cancellationToken = default)
            {
                _requestedUrls.Add(url);
                AwaitableCompletionSource<Response<T>> completion = new();
                if (Fail)
                {
                    completion.SetResult(Response<T>.Failure("offline", 503));
                }
                else
                {
                    completion.SetResult(Response<T>.Success((T)(object)new Todo { title = "buy milk" }));
                }

                return completion.Awaitable;
            }

            public Awaitable<Response<string>> GetAsync(string url, RequestOptions options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Awaitable<Response<byte[]>> GetBytesAsync(string url, RequestOptions options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Awaitable<Response<string>> PostAsync(string url, string body, RequestOptions options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Awaitable<Response<string>> PutAsync(string url, string body, RequestOptions options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Awaitable<Response<string>> DeleteAsync(string url, RequestOptions options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Awaitable<Response<IReadOnlyDictionary<string, string>>> HeadAsync(string url, RequestOptions options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Awaitable<Response<string>> SendAsync(HttpMethodType method, string url, string body, RequestOptions options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Awaitable<Response<TResponse>> PostJsonAsync<TRequest, TResponse>(string url, TRequest body, RequestOptions options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Awaitable<Response<TResponse>> PutJsonAsync<TRequest, TResponse>(string url, TRequest body, RequestOptions options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Awaitable<Response<Texture2D>> DownloadTextureAsync(string url, bool readable = false, RequestOptions options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Awaitable<Response<AudioClip>> DownloadAudioAsync(string url, AudioType audioType, RequestOptions options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Awaitable<Response<string>> DownloadFileAsync(string url, string path, RequestOptions options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }
    }
}
