using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Pretzel.Core;
using Pretzel.Data;
using UnityEngine;
using UnityEngine.Networking;

namespace Pretzel.Api
{
    /// <summary>
    /// HTTP client on <see cref="UnityWebRequest"/> and <see cref="Awaitable"/>. Call it from the main thread.
    /// Every method takes a <see cref="CancellationToken"/>. Cancelling aborts the request and throws <see cref="OperationCanceledException"/>.
    /// </summary>
    public sealed class ApiClient
    {
        private const string JsonContentType = "application/json";

        private static readonly UTF8Encoding Utf8 = new(false);

        private readonly ISerializer _serializer;

        /// <summary>Prepended to relative URLs.</summary>
        public string BaseUrl { get; set; }

        /// <summary>Settings used when a call does not override them. Starts with a 30 second timeout and no retries.</summary>
        public RequestOptions Defaults { get; } = new() { TimeoutSeconds = 30f, Retries = 0, RetryDelaySeconds = 0.5f, ContentType = JsonContentType };

        /// <param name="serializer">Used by the JSON methods. Defaults to a <see cref="NewtonsoftSerializer"/>.</param>
        public ApiClient(string baseUrl = null, ISerializer serializer = null)
        {
            BaseUrl = baseUrl;
            _serializer = serializer ?? new NewtonsoftSerializer();
        }

        #region TEXT AND BYTES

        public Awaitable<Response<string>> GetAsync(string url, RequestOptions options = null, CancellationToken cancellationToken = default)
        {
            return SendAsync(HttpMethodType.GET, url, null, options, cancellationToken);
        }

        public Awaitable<Response<byte[]>> GetBytesAsync(string url, RequestOptions options = null, CancellationToken cancellationToken = default)
        {
            string target = ResolveUrl(url);
            return ExecuteAsync(() => Build("GET", target, null, null, new DownloadHandlerBuffer()), r => r.downloadHandler.data, true, options, cancellationToken);
        }

        public Awaitable<Response<string>> PostAsync(string url, string body, RequestOptions options = null, CancellationToken cancellationToken = default)
        {
            return SendAsync(HttpMethodType.POST, url, body, options, cancellationToken);
        }

        public Awaitable<Response<string>> PutAsync(string url, string body, RequestOptions options = null, CancellationToken cancellationToken = default)
        {
            return SendAsync(HttpMethodType.PUT, url, body, options, cancellationToken);
        }

        public Awaitable<Response<string>> DeleteAsync(string url, RequestOptions options = null, CancellationToken cancellationToken = default)
        {
            return SendAsync(HttpMethodType.DELETE, url, null, options, cancellationToken);
        }

        /// <summary>The response headers are in <see cref="Response{T}.Headers"/>. <c>Data</c> is the same dictionary.</summary>
        public async Awaitable<Response<IReadOnlyDictionary<string, string>>> HeadAsync(string url, RequestOptions options = null, CancellationToken cancellationToken = default)
        {
            string target = ResolveUrl(url);
            Response<bool> response = await ExecuteAsync(() => UnityWebRequest.Head(target), _ => true, false, options, cancellationToken);
            return new Response<IReadOnlyDictionary<string, string>>(
                response.Ok, response.StatusCode, response.Ok ? response.Headers : null, response.Error, null, response.TimedOut, response.Headers);
        }

        /// <summary>Sends any method with an optional text body and returns the response text.</summary>
        public Awaitable<Response<string>> SendAsync(HttpMethodType method, string url, string body, RequestOptions options = null, CancellationToken cancellationToken = default)
        {
            string target = ResolveUrl(url);
            string contentType = Merge(options).ContentType;
            byte[] payload = string.IsNullOrEmpty(body) ? null : Utf8.GetBytes(body);

            if (method == HttpMethodType.HEAD)
            {
                return ExecuteAsync(() => UnityWebRequest.Head(target), _ => string.Empty, false, options, cancellationToken);
            }

            return ExecuteAsync(
                () => Build(method.ToString(), target, payload, contentType, new DownloadHandlerBuffer()),
                r => r.downloadHandler.text,
                true,
                options,
                cancellationToken);
        }

        #endregion

        #region JSON

        public async Awaitable<Response<T>> GetJsonAsync<T>(string url, RequestOptions options = null, CancellationToken cancellationToken = default)
        {
            return ParseJson<T>(await GetAsync(url, options, cancellationToken));
        }

        public async Awaitable<Response<TResponse>> PostJsonAsync<TRequest, TResponse>(string url, TRequest body, RequestOptions options = null, CancellationToken cancellationToken = default)
        {
            return ParseJson<TResponse>(await PostAsync(url, _serializer.Serialize(body, false), options, cancellationToken));
        }

        public async Awaitable<Response<TResponse>> PutJsonAsync<TRequest, TResponse>(string url, TRequest body, RequestOptions options = null, CancellationToken cancellationToken = default)
        {
            return ParseJson<TResponse>(await PutAsync(url, _serializer.Serialize(body, false), options, cancellationToken));
        }

        private Response<T> ParseJson<T>(Response<string> response)
        {
            if (!response.Ok)
            {
                return new Response<T>(false, response.StatusCode, default, response.Error, response.ErrorBody, response.TimedOut, response.Headers);
            }

            try
            {
                T data = _serializer.Deserialize<T>(response.Data);
                return new Response<T>(true, response.StatusCode, data, null, null, false, response.Headers);
            }
            catch (Exception ex)
            {
                return new Response<T>(false, response.StatusCode, default, "Invalid JSON: " + ex.Message, response.Data, false, response.Headers);
            }
        }

        #endregion

        #region DOWNLOADS

        /// <summary>The texture is not re-encoded. Pass <paramref name="readable"/> only when you need CPU access to the pixels.</summary>
        public Awaitable<Response<Texture2D>> DownloadTextureAsync(string url, bool readable = false, RequestOptions options = null, CancellationToken cancellationToken = default)
        {
            string target = ResolveUrl(url);
            return ExecuteAsync(
                () => UnityWebRequestTexture.GetTexture(target, !readable),
                DownloadHandlerTexture.GetContent,
                false,
                options,
                cancellationToken);
        }

        public Awaitable<Response<AudioClip>> DownloadAudioAsync(string url, AudioType audioType, RequestOptions options = null, CancellationToken cancellationToken = default)
        {
            string target = ResolveUrl(url);
            return ExecuteAsync(
                () => UnityWebRequestMultimedia.GetAudioClip(target, audioType),
                DownloadHandlerAudioClip.GetContent,
                false,
                options,
                cancellationToken);
        }

        /// <summary>
        /// Streams to disk through <see cref="DownloadHandlerFile"/>, so large files never sit in memory. Returns the file path.
        /// The download goes to a temporary file first, so a failed or cancelled request leaves an existing file untouched.
        /// </summary>
        public async Awaitable<Response<string>> DownloadFileAsync(string url, string path, RequestOptions options = null, CancellationToken cancellationToken = default)
        {
            Guard.NotNullOrEmpty(path, nameof(path));
            string target = ResolveUrl(url);
            string fullPath = Path.GetFullPath(path);
            string tempPath = fullPath + ".download";
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            try
            {
                Response<string> response = await ExecuteAsync(
                    () => Build("GET", target, null, null, new DownloadHandlerFile(tempPath) { removeFileOnAbort = true }),
                    _ => fullPath,
                    false,
                    options,
                    cancellationToken);

                if (!response.Ok)
                {
                    return response;
                }

                try
                {
                    ReplaceFile(tempPath, fullPath);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    return new Response<string>(false, response.StatusCode, null, "Could not write the file: " + ex.Message, null, false, response.Headers);
                }

                return response;
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        private static void ReplaceFile(string source, string destination)
        {
            if (!File.Exists(destination))
            {
                File.Move(source, destination);
                return;
            }

            try
            {
                File.Replace(source, destination, null);
            }
            catch (NotSupportedException)
            {
                File.Delete(destination);
                File.Move(source, destination);
            }
        }

        #endregion

        #region CORE

        private async Awaitable<Response<T>> ExecuteAsync<T>(
            Func<UnityWebRequest> create,
            Func<UnityWebRequest, T> read,
            bool captureErrorBody,
            RequestOptions options,
            CancellationToken cancellationToken)
        {
            RequestOptions settings = Merge(options);
            int attempts = Mathf.Max(0, settings.Retries ?? 0) + 1;
            Response<T> last = null;

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (attempt > 0)
                {
                    float delay = (settings.RetryDelaySeconds ?? 0.5f) * Mathf.Pow(2f, attempt - 1);
                    await Awaitable.WaitForSecondsAsync(delay, cancellationToken);
                }

                using UnityWebRequest request = create();
                foreach (KeyValuePair<string, string> header in settings.Headers)
                {
                    request.SetRequestHeader(header.Key, header.Value);
                }

                request.timeout = Mathf.CeilToInt(Mathf.Max(0f, settings.TimeoutSeconds ?? 0f));

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                float reported = -1f;
                while (!operation.isDone)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        request.Abort();
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    if (settings.Progress != null && !Mathf.Approximately(operation.progress, reported))
                    {
                        reported = operation.progress;
                        settings.Progress.Report(reported);
                    }

                    await Awaitable.NextFrameAsync(cancellationToken);
                }

                settings.Progress?.Report(1f);
                last = ToResponse(request, read, captureErrorBody);
                if (last.Ok || !IsRetryable(request))
                {
                    return last;
                }
            }

            return last;
        }

        private static Response<T> ToResponse<T>(UnityWebRequest request, Func<UnityWebRequest, T> read, bool captureErrorBody)
        {
            Dictionary<string, string> raw = request.GetResponseHeaders();
            IReadOnlyDictionary<string, string> headers = raw == null ? null : new Dictionary<string, string>(raw, StringComparer.OrdinalIgnoreCase);

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    return new Response<T>(true, request.responseCode, read(request), null, null, false, headers);
                }
                catch (Exception ex)
                {
                    return new Response<T>(false, request.responseCode, default, "Could not read the response: " + ex.Message, null, false, headers);
                }
            }

            string errorBody = null;
            if (captureErrorBody && request.downloadHandler is DownloadHandlerBuffer buffer)
            {
                errorBody = buffer.text;
            }

            bool timedOut = request.result == UnityWebRequest.Result.ConnectionError
                            && request.error != null
                            && request.error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0;
            return new Response<T>(false, request.responseCode, default, request.error, errorBody, timedOut, headers);
        }

        private static bool IsRetryable(UnityWebRequest request)
        {
            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                return true;
            }

            long status = request.responseCode;
            return status == 408 || status == 429 || status >= 500;
        }

        private static UnityWebRequest Build(string method, string url, byte[] body, string contentType, DownloadHandler downloadHandler)
        {
            UploadHandler upload = body == null ? null : new UploadHandlerRaw(body) { contentType = contentType ?? JsonContentType };
            return new UnityWebRequest(url, method, downloadHandler, upload);
        }

        private RequestOptions Merge(RequestOptions call)
        {
            RequestOptions merged = new()
            {
                TimeoutSeconds = call?.TimeoutSeconds ?? Defaults.TimeoutSeconds,
                Retries = call?.Retries ?? Defaults.Retries,
                RetryDelaySeconds = call?.RetryDelaySeconds ?? Defaults.RetryDelaySeconds,
                ContentType = call?.ContentType ?? Defaults.ContentType ?? JsonContentType,
                Progress = call?.Progress
            };

            foreach (KeyValuePair<string, string> header in Defaults.Headers)
            {
                merged.Headers[header.Key] = header.Value;
            }

            if (call != null)
            {
                foreach (KeyValuePair<string, string> header in call.Headers)
                {
                    merged.Headers[header.Key] = header.Value;
                }
            }

            return merged;
        }

        private string ResolveUrl(string url)
        {
            Guard.NotNullOrEmpty(url, nameof(url));
            // Uri.TryCreate treats "/path" as an absolute file URI on Linux and macOS, so check for a scheme.
            if (url.IndexOf("://", StringComparison.Ordinal) > 0)
            {
                return url;
            }

            if (string.IsNullOrEmpty(BaseUrl))
            {
                throw new ArgumentException($"'{url}' is relative and no BaseUrl is set.", nameof(url));
            }

            return BaseUrl.TrimEnd('/') + "/" + url.TrimStart('/');
        }

        #endregion
    }
}
