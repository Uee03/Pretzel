using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Pretzel.Api
{
    /// <summary>
    /// What code needs from an HTTP client. <see cref="ApiClient"/> is the implementation; depend on this interface in services
    /// and view models so tests can return canned <see cref="Response{T}"/> values without a network.
    /// </summary>
    public interface IApiClient
    {
        /// <summary>Prepended to relative URLs.</summary>
        string BaseUrl { get; set; }

        /// <summary>Settings used when a call does not pass its own <see cref="RequestOptions"/>.</summary>
        RequestOptions Defaults { get; }

        Awaitable<Response<string>> GetAsync(string url, RequestOptions options = null, CancellationToken cancellationToken = default);

        Awaitable<Response<byte[]>> GetBytesAsync(string url, RequestOptions options = null, CancellationToken cancellationToken = default);

        Awaitable<Response<string>> PostAsync(string url, string body, RequestOptions options = null, CancellationToken cancellationToken = default);

        Awaitable<Response<string>> PutAsync(string url, string body, RequestOptions options = null, CancellationToken cancellationToken = default);

        Awaitable<Response<string>> DeleteAsync(string url, RequestOptions options = null, CancellationToken cancellationToken = default);

        Awaitable<Response<IReadOnlyDictionary<string, string>>> HeadAsync(string url, RequestOptions options = null, CancellationToken cancellationToken = default);

        Awaitable<Response<string>> SendAsync(HttpMethodType method, string url, string body, RequestOptions options = null, CancellationToken cancellationToken = default);

        Awaitable<Response<T>> GetJsonAsync<T>(string url, RequestOptions options = null, CancellationToken cancellationToken = default);

        Awaitable<Response<TResponse>> PostJsonAsync<TRequest, TResponse>(string url, TRequest body, RequestOptions options = null, CancellationToken cancellationToken = default);

        Awaitable<Response<TResponse>> PutJsonAsync<TRequest, TResponse>(string url, TRequest body, RequestOptions options = null, CancellationToken cancellationToken = default);

        Awaitable<Response<Texture2D>> DownloadTextureAsync(string url, bool readable = false, RequestOptions options = null, CancellationToken cancellationToken = default);

        Awaitable<Response<AudioClip>> DownloadAudioAsync(string url, AudioType audioType, RequestOptions options = null, CancellationToken cancellationToken = default);

        Awaitable<Response<string>> DownloadFileAsync(string url, string path, RequestOptions options = null, CancellationToken cancellationToken = default);
    }
}
