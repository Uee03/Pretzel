using System.Threading;
using UnityEngine;

namespace Pretzel.Cloud
{
    /// <summary>
    /// Calls server-side Cloud Code. Requests and responses are converted with the Pretzel Data <c>ISerializer</c> the service was built with,
    /// so the request type's fields are the endpoint's parameter names and the response type matches the endpoint's JSON result.
    /// Failures are results, cancellation throws. All members must be used from the main thread.
    /// </summary>
    public interface ICloudCodeService
    {
        /// <summary>Calls <paramref name="function"/> in a Cloud Code module. <paramref name="request"/> may be null for an endpoint without parameters.</summary>
        Awaitable<CloudResult<TResponse>> CallModuleAsync<TRequest, TResponse>(string module, string function, TRequest request, CancellationToken cancellationToken = default);

        /// <summary>Calls a Cloud Code script endpoint by name. <paramref name="request"/> may be null for a script without parameters.</summary>
        Awaitable<CloudResult<TResponse>> CallScriptAsync<TRequest, TResponse>(string script, TRequest request, CancellationToken cancellationToken = default);
    }
}
