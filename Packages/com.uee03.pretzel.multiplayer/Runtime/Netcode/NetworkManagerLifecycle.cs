using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace Pretzel.Multiplayer
{
    /// <summary>Awaitable helpers around NetworkManager, whose Shutdown completes on a later frame.</summary>
    internal static class NetworkManagerLifecycle
    {
        private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(5);

        /// <summary>Calls Shutdown when the manager is running and waits until it has stopped. Returns false when it did not stop within the timeout.</summary>
        public static async Task<bool> ShutdownAsync(NetworkManager networkManager, CancellationToken cancellationToken, TimeSpan? timeout = null)
        {
            if (networkManager == null)
            {
                return true;
            }

            if (networkManager.IsListening && !networkManager.ShutdownInProgress)
            {
                networkManager.Shutdown();
            }

            float deadline = Time.realtimeSinceStartup + (float)(timeout ?? _defaultTimeout).TotalSeconds;
            while (networkManager != null && (networkManager.IsListening || networkManager.ShutdownInProgress))
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    return false;
                }

                await Awaitable.NextFrameAsync(cancellationToken);
            }

            return true;
        }

        public static void SetConnectionData(NetworkManager networkManager, ConnectionPayload payload, NetworkSettings settings)
        {
            networkManager.NetworkConfig.ConnectionData = ConnectionPayloadCodec.Encode(payload, settings.MaxConnectionDataBytes);
        }
    }
}
