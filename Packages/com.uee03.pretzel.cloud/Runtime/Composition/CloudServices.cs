using System;
using System.Threading;
using Pretzel.Core;
using Pretzel.Data;
using UnityEngine;

namespace Pretzel.Cloud
{
    /// <summary>
    /// The cloud services a game uses, wired once in the composition root. Plain object: no static state, no singleton.
    /// Build it with <see cref="CreateBuilder"/>; each adapter assembly adds its own <c>Use...</c> extension on <see cref="CloudServicesBuilder"/>
    /// (for example <c>UseUnityCloudSave</c>), and those extensions exist only when the matching Unity package is installed.
    /// A service you did not add is null.
    /// </summary>
    public sealed class CloudServices
    {
        private readonly CloudRetry _retry;

        public ISerializer Serializer { get; }

        public ICloudAuthService Auth { get; }

        public ICloudSaveService Save { get; }

        public ICloudCodeService Code { get; }

        public IRemoteConfigService RemoteConfig { get; }

        /// <summary>The retry policy and delay the builder was given, for your own calls through <see cref="CloudRetry"/>.</summary>
        public CloudRetry Retry => _retry;

        internal CloudServices(ISerializer serializer, ICloudAuthService auth, ICloudSaveService save, ICloudCodeService code, IRemoteConfigService remoteConfig, CloudRetry retry)
        {
            Serializer = serializer;
            Auth = auth;
            Save = save;
            Code = code;
            RemoteConfig = remoteConfig;
            _retry = retry;
        }

        public static CloudServicesBuilder CreateBuilder(ISerializer serializer = null) => new(serializer);

        /// <summary>
        /// Initializes the authentication service and signs in anonymously unless the player is already signed in, retrying transient failures.
        /// Fails with <see cref="CloudErrorKind.NotInitialized"/> when no authentication service was added.
        /// </summary>
        public async Awaitable<CloudResult> ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (Auth == null)
            {
                return CloudResult.Failure(CloudErrorKind.NotInitialized, "No authentication service was added to CloudServices.");
            }

            CloudResult initialized = await _retry.RunAsync(token => Auth.InitializeAsync(token), cancellationToken);
            if (!initialized.IsSuccess)
            {
                return initialized;
            }

            return await _retry.RunAsync(token => Auth.SignInAnonymouslyAsync(token), cancellationToken);
        }

        /// <summary>Wraps <paramref name="local"/> so saves are queued for the cloud. Assign it to <c>DataIO.Default</c> to route the whole game through it.</summary>
        /// <exception cref="InvalidOperationException">No cloud save service was added.</exception>
        public SyncedDataStore CreateSyncedStore(IDataStore local, SyncedDataStoreOptions options = null)
        {
            Guard.NotNull(local, nameof(local));
            if (Save == null)
            {
                throw new InvalidOperationException("No cloud save service was added to CloudServices.");
            }

            return new SyncedDataStore(local, Save, options);
        }
    }
}
