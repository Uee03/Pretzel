using Pretzel.Cloud.RemoteConfig;
using Pretzel.Core;

namespace Pretzel.Cloud
{
    /// <summary>Adds Remote Config to <see cref="CloudServicesBuilder"/>. Exists only when the Remote Config package is installed.</summary>
    public static class UnityRemoteConfigBuilderExtensions
    {
        /// <summary>Uses the builder's <see cref="CloudServicesBuilder.Serializer"/> for <see cref="IRemoteConfigService.GetJson{T}"/>.</summary>
        public static CloudServicesBuilder UseUnityRemoteConfig(this CloudServicesBuilder builder)
        {
            Guard.NotNull(builder, nameof(builder));
            return builder.UseRemoteConfig(new UnityRemoteConfigService(builder.Serializer));
        }
    }
}
