using System;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pretzel.Core;
using Pretzel.Data;
using Unity.Services.CloudCode;
using UnityEngine;
using SdkCloudCodeService = Unity.Services.CloudCode.ICloudCodeService;

namespace Pretzel.Cloud.CloudCode
{
    /// <summary>
    /// <see cref="Pretzel.Cloud.ICloudCodeService"/> on Unity Cloud Code. The request is serialized with the Pretzel <see cref="ISerializer"/>
    /// and sent as the endpoint's named arguments, so its JSON must be an object whose keys are the endpoint's parameter names.
    /// The response is read as JSON and deserialized with the same serializer. The SDK calls take no cancellation token, so cancelling
    /// stops the wait but a request already sent still runs on the server. Use from the main thread.
    /// </summary>
    public sealed class UnityCloudCodeService : Pretzel.Cloud.ICloudCodeService
    {
        private readonly ISerializer _serializer;
        private readonly Func<SdkCloudCodeService> _service;

        /// <summary>Uses <c>CloudCodeService.Instance</c>, resolved on every call so it works once Unity Services are initialized.</summary>
        public UnityCloudCodeService(ISerializer serializer)
            : this(serializer, () => CloudCodeService.Instance)
        {
        }

        /// <param name="service">Supplies the SDK's Cloud Code service. Use it to inject a stub in tests.</param>
        public UnityCloudCodeService(ISerializer serializer, Func<SdkCloudCodeService> service)
        {
            _serializer = Guard.NotNull(serializer, nameof(serializer));
            _service = Guard.NotNull(service, nameof(service));
        }

        public Awaitable<CloudResult<TResponse>> CallModuleAsync<TRequest, TResponse>(string module, string function, TRequest request, CancellationToken cancellationToken = default)
        {
            Guard.NotNullOrEmpty(module, nameof(module));
            Guard.NotNullOrEmpty(function, nameof(function));
            return CallAsync<TRequest, TResponse>(module, function, request, cancellationToken);
        }

        public Awaitable<CloudResult<TResponse>> CallScriptAsync<TRequest, TResponse>(string script, TRequest request, CancellationToken cancellationToken = default)
        {
            Guard.NotNullOrEmpty(script, nameof(script));
            return CallAsync<TRequest, TResponse>(null, script, request, cancellationToken);
        }

        private async Awaitable<CloudResult<TResponse>> CallAsync<TRequest, TResponse>(string module, string name, TRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryBuildArguments(request, out Dictionary<string, object> arguments, out CloudError argumentError))
            {
                return CloudResult<TResponse>.Failure(argumentError);
            }

            try
            {
                SdkCloudCodeService service = _service();
                JToken output = module == null
                    ? await service.CallEndpointAsync<JToken>(name, arguments).WithCancellationAsync(cancellationToken)
                    : await service.CallModuleEndpointAsync<JToken>(module, name, arguments).WithCancellationAsync(cancellationToken);
                string json = output == null ? "null" : output.ToString(Formatting.None);
                return CloudResult<TResponse>.Success(_serializer.Deserialize<TResponse>(json));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return CloudResult<TResponse>.Failure(UnityCloudCodeErrors.Map(ex));
            }
        }

        private bool TryBuildArguments<TRequest>(TRequest request, out Dictionary<string, object> arguments, out CloudError error)
        {
            arguments = null;
            error = null;
            if (request == null)
            {
                return true;
            }

            try
            {
                string json = _serializer.Serialize(request, false);
                arguments = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                return true;
            }
            catch (Exception ex) when (CloudSerialization.IsFailure(ex))
            {
                error = new CloudError(CloudErrorKind.InvalidRequest, $"The request must serialize to a JSON object of named arguments: {ex.Message}");
                return false;
            }
        }
    }
}
