using System;
using System.Collections.Generic;
using System.Threading;
using Pretzel.Cloud;
using UnityEngine;

namespace Pretzel.Samples.Cloud.CodeAndConfig
{
    /// <summary>
    /// Calls a Cloud Code module endpoint and reads Remote Config values. The module name, function and config keys below are
    /// examples: create them in your own project (see the README of this sample), or change the serialized fields.
    /// Results go to the Console.
    /// </summary>
    public sealed class CloudCodeAndRemoteConfigExample : MonoBehaviour
    {
        [SerializeField] private string _module = "HelloWorld";
        [SerializeField] private string _function = "SayHello";
        [SerializeField] private string _welcomeKey = "welcome_message";
        [SerializeField] private string _tuningKey = "game_tuning";

        private CloudServices _services;

        private async void Start()
        {
            // Start is a Unity message, so this is the one place an async void is needed.
            try
            {
                await RunAsync(destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // The component was destroyed; nothing to report.
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
            }
        }

        private async Awaitable RunAsync(CancellationToken cancellationToken)
        {
            // Composition root. The serializer passed to the builder converts Cloud Code payloads and Remote Config JSON.
            _services = CloudServices.CreateBuilder()
                .UseUnityAuthentication()
                .UseUnityCloudCode()
                .UseUnityRemoteConfig()
                .Build();

            CloudResult connected = await _services.ConnectAsync(cancellationToken);
            if (!connected.IsSuccess)
            {
                Debug.LogWarning($"Not connected: {connected.Error}", this);
                return;
            }

            await ReadRemoteConfigAsync(cancellationToken);
            await CallEndpointAsync(cancellationToken);
        }

        private async Awaitable ReadRemoteConfigAsync(CancellationToken cancellationToken)
        {
            Dictionary<string, object> userAttributes = new(StringComparer.Ordinal) { ["level"] = 1 };
            CloudResult<RemoteConfigFetchInfo> fetched = await _services.RemoteConfig.FetchAsync(userAttributes, null, cancellationToken);
            if (!fetched.IsSuccess)
            {
                Debug.LogWarning($"Remote Config fetch failed: {fetched.Error}", this);
                return;
            }

            // A failed network fetch does not fail the call: the SDK keeps cached values. IsFresh tells them apart.
            Debug.Log($"Remote Config: {fetched.Value.KeyCount} keys, origin {fetched.Value.Origin}, fresh {fetched.Value.IsFresh}.", this);

            // Getters never throw; they return the default you pass when the key is missing or has another type.
            string welcome = _services.RemoteConfig.GetString(_welcomeKey, "Welcome!");
            GameTuning tuning = _services.RemoteConfig.GetJson(_tuningKey, new GameTuning());
            Debug.Log($"{welcome} Enemies: {tuning.maxEnemies} at speed {tuning.enemySpeed}.", this);
        }

        private async Awaitable CallEndpointAsync(CancellationToken cancellationToken)
        {
            CloudResult<GreetingResponse> result = await _services.Code.CallModuleAsync<GreetingRequest, GreetingResponse>(_module, _function, new GreetingRequest { name = "Pretzel" }, cancellationToken);
            if (result.IsSuccess)
            {
                Debug.Log($"Cloud Code says: {result.Value.message}", this);
                return;
            }

            // Branch on the error kind, never on SDK exception types.
            switch (result.Error.Kind)
            {
                case CloudErrorKind.NotFound:
                    Debug.LogWarning($"Module '{_module}' or function '{_function}' is not deployed in this project.", this);
                    break;
                case CloudErrorKind.ScriptError:
                    Debug.LogWarning($"The endpoint ran and reported an error: {result.Error.Message}", this);
                    break;
                default:
                    Debug.LogWarning($"Cloud Code call failed: {result.Error}", this);
                    break;
            }
        }
    }
}
