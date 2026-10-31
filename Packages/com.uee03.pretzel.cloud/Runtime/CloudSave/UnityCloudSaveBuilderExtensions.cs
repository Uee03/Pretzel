using Pretzel.Cloud.CloudSave;
using Pretzel.Core;

namespace Pretzel.Cloud
{
    /// <summary>Adds Cloud Save to <see cref="CloudServicesBuilder"/>. Exists only when the Cloud Save package is installed.</summary>
    public static class UnityCloudSaveBuilderExtensions
    {
        public static CloudServicesBuilder UseUnityCloudSave(this CloudServicesBuilder builder)
        {
            Guard.NotNull(builder, nameof(builder));
            return builder.UseSave(new UnityCloudSaveService());
        }
    }
}
