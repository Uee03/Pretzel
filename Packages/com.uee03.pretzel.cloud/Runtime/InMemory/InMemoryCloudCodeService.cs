using System;
using System.Collections.Generic;
using System.Threading;
using Pretzel.Core;
using Pretzel.Data;
using UnityEngine;

namespace Pretzel.Cloud.InMemory
{
    /// <summary>
    /// An in-memory <see cref="ICloudCodeService"/>: register a C# handler per endpoint. Requests and responses cross the endpoint as serializer text,
    /// like the real service, so a call whose types do not match the handler's fails the same way.
    /// </summary>
    public sealed class InMemoryCloudCodeService : ICloudCodeService
    {
        private readonly ISerializer _serializer;
        private readonly Dictionary<string, Func<string, string>> _handlers = new(StringComparer.Ordinal);

        public CloudFaults Faults { get; } = new();

        /// <summary>The serialized request of the last call that reached a handler, or null.</summary>
        public string LastRequestJson { get; private set; }

        public InMemoryCloudCodeService(ISerializer serializer)
        {
            _serializer = Guard.NotNull(serializer, nameof(serializer));
        }

        public void RegisterModule<TRequest, TResponse>(string module, string function, Func<TRequest, TResponse> handler)
        {
            Register(ModuleKey(module, function), handler);
        }

        public void RegisterScript<TRequest, TResponse>(string script, Func<TRequest, TResponse> handler)
        {
            Register(ScriptKey(script), handler);
        }

        public Awaitable<CloudResult<TResponse>> CallModuleAsync<TRequest, TResponse>(string module, string function, TRequest request, CancellationToken cancellationToken = default)
        {
            return CallAsync<TRequest, TResponse>(ModuleKey(module, function), request, cancellationToken);
        }

        public Awaitable<CloudResult<TResponse>> CallScriptAsync<TRequest, TResponse>(string script, TRequest request, CancellationToken cancellationToken = default)
        {
            return CallAsync<TRequest, TResponse>(ScriptKey(script), request, cancellationToken);
        }

        private void Register<TRequest, TResponse>(string key, Func<TRequest, TResponse> handler)
        {
            Guard.NotNull(handler, nameof(handler));
            _handlers[key] = json =>
            {
                TRequest request = json == null ? default : _serializer.Deserialize<TRequest>(json);
                return _serializer.Serialize(handler(request), false);
            };
        }

        private async Awaitable<CloudResult<TResponse>> CallAsync<TRequest, TResponse>(string key, TRequest request, CancellationToken cancellationToken)
        {
            await Awaitable.MainThreadAsync();
            cancellationToken.ThrowIfCancellationRequested();
            CloudError fault = Faults.Next();
            if (fault != null)
            {
                return CloudResult<TResponse>.Failure(fault);
            }

            if (!_handlers.TryGetValue(key, out Func<string, string> handler))
            {
                return CloudResult<TResponse>.Failure(CloudErrorKind.NotFound, $"No handler registered for '{key}'.");
            }

            try
            {
                string requestJson = request == null ? null : _serializer.Serialize(request, false);
                LastRequestJson = requestJson;
                string responseJson = handler(requestJson);
                return CloudResult<TResponse>.Success(_serializer.Deserialize<TResponse>(responseJson));
            }
            catch (Exception ex) when (CloudSerialization.IsFailure(ex))
            {
                return CloudResult<TResponse>.Failure(CloudErrorKind.Serialization, ex.Message);
            }
        }

        private static string ModuleKey(string module, string function)
        {
            return "module:" + Guard.NotNullOrEmpty(module, nameof(module)) + "/" + Guard.NotNullOrEmpty(function, nameof(function));
        }

        private static string ScriptKey(string script) => "script:" + Guard.NotNullOrEmpty(script, nameof(script));
    }
}
