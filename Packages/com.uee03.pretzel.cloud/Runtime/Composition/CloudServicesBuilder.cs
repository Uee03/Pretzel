using Pretzel.Core;
using Pretzel.Data;

namespace Pretzel.Cloud
{
    /// <summary>Collects the cloud services for <see cref="CloudServices"/>. Adapter assemblies add <c>Use...</c> extension methods to it.</summary>
    public sealed class CloudServicesBuilder
    {
        private ICloudAuthService _auth;
        private ICloudSaveService _save;
        private ICloudCodeService _code;
        private IRemoteConfigService _remoteConfig;
        private RetryPolicy _retryPolicy;
        private IRetryDelay _retryDelay;

        /// <summary>The serializer adapters use for Cloud Code payloads and Remote Config JSON. Defaults to <see cref="NewtonsoftSerializer"/>.</summary>
        public ISerializer Serializer { get; }

        public CloudServicesBuilder(ISerializer serializer = null)
        {
            Serializer = serializer ?? new NewtonsoftSerializer();
        }

        public CloudServicesBuilder UseAuth(ICloudAuthService auth)
        {
            _auth = Guard.NotNull(auth, nameof(auth));
            return this;
        }

        public CloudServicesBuilder UseSave(ICloudSaveService save)
        {
            _save = Guard.NotNull(save, nameof(save));
            return this;
        }

        public CloudServicesBuilder UseCode(ICloudCodeService code)
        {
            _code = Guard.NotNull(code, nameof(code));
            return this;
        }

        public CloudServicesBuilder UseRemoteConfig(IRemoteConfigService remoteConfig)
        {
            _remoteConfig = Guard.NotNull(remoteConfig, nameof(remoteConfig));
            return this;
        }

        /// <summary>Sets the retry used by <see cref="CloudServices.ConnectAsync"/>. Defaults to three attempts with backoff.</summary>
        public CloudServicesBuilder WithRetry(RetryPolicy policy, IRetryDelay delay = null)
        {
            _retryPolicy = Guard.NotNull(policy, nameof(policy));
            _retryDelay = delay;
            return this;
        }

        public CloudServices Build()
        {
            return new CloudServices(Serializer, _auth, _save, _code, _remoteConfig, new CloudRetry(_retryPolicy, _retryDelay));
        }
    }
}
