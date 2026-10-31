using Pretzel.Cloud.CloudCode;
using Pretzel.Core;

namespace Pretzel.Cloud
{
    /// <summary>Adds Cloud Code to <see cref="CloudServicesBuilder"/>. Exists only when the Cloud Code package is installed.</summary>
    public static class UnityCloudCodeBuilderExtensions
    {
        /// <summary>Uses the builder's <see cref="CloudServicesBuilder.Serializer"/> for requests and responses.</summary>
        public static CloudServicesBuilder UseUnityCloudCode(this CloudServicesBuilder builder)
        {
            Guard.NotNull(builder, nameof(builder));
            return builder.UseCode(new UnityCloudCodeService(builder.Serializer));
        }
    }
}
