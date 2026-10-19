namespace Pretzel.Integrations.R3
{
    using global::R3;
    using Pretzel.Api;

    /// <summary>
    /// Cold observables over <see cref="ApiClient"/>. Each subscription sends one request, emits the response and completes.
    /// Disposing the subscription cancels the request.
    /// </summary>
    public static class ApiClientObservableExtensions
    {
        public static Observable<Response<string>> GetAsObservable(this ApiClient client, string url, RequestOptions options = null)
        {
            return Observable.FromAsync<Response<string>>(async ct => await client.GetAsync(url, options, ct));
        }

        public static Observable<Response<T>> GetJsonAsObservable<T>(this ApiClient client, string url, RequestOptions options = null)
        {
            return Observable.FromAsync<Response<T>>(async ct => await client.GetJsonAsync<T>(url, options, ct));
        }

        public static Observable<Response<TResponse>> PostJsonAsObservable<TRequest, TResponse>(this ApiClient client, string url, TRequest body, RequestOptions options = null)
        {
            return Observable.FromAsync<Response<TResponse>>(async ct => await client.PostJsonAsync<TRequest, TResponse>(url, body, options, ct));
        }
    }
}
