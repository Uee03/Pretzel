namespace Pretzel.Integrations.UniTask
{
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using Pretzel.Api;
    using UnityEngine;

    /// <summary>UniTask versions of the <see cref="ApiClient"/> methods that return <see cref="Awaitable"/>.</summary>
    public static class ApiClientUniTaskExtensions
    {
        public static UniTask<Response<string>> GetUniTask(this ApiClient client, string url, RequestOptions options = null, CancellationToken cancellationToken = default)
        {
            return client.GetAsync(url, options, cancellationToken).AsUniTask();
        }

        public static UniTask<Response<string>> PostUniTask(this ApiClient client, string url, string body, RequestOptions options = null, CancellationToken cancellationToken = default)
        {
            return client.PostAsync(url, body, options, cancellationToken).AsUniTask();
        }

        public static UniTask<Response<T>> GetJsonUniTask<T>(this ApiClient client, string url, RequestOptions options = null, CancellationToken cancellationToken = default)
        {
            return client.GetJsonAsync<T>(url, options, cancellationToken).AsUniTask();
        }

        public static UniTask<Response<TResponse>> PostJsonUniTask<TRequest, TResponse>(this ApiClient client, string url, TRequest body, RequestOptions options = null, CancellationToken cancellationToken = default)
        {
            return client.PostJsonAsync<TRequest, TResponse>(url, body, options, cancellationToken).AsUniTask();
        }

        public static UniTask<Response<Texture2D>> DownloadTextureUniTask(this ApiClient client, string url, bool readable = false, RequestOptions options = null, CancellationToken cancellationToken = default)
        {
            return client.DownloadTextureAsync(url, readable, options, cancellationToken).AsUniTask();
        }

        public static UniTask<Response<string>> DownloadFileUniTask(this ApiClient client, string url, string path, RequestOptions options = null, CancellationToken cancellationToken = default)
        {
            return client.DownloadFileAsync(url, path, options, cancellationToken).AsUniTask();
        }
    }
}
