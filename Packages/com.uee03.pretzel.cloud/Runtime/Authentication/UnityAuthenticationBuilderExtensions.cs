using Pretzel.Core;
using Pretzel.Cloud.Authentication;
using Unity.Services.Core;

namespace Pretzel.Cloud
{
    /// <summary>Adds Unity Authentication to <see cref="CloudServicesBuilder"/>. Exists only when the Authentication package is installed.</summary>
    public static class UnityAuthenticationBuilderExtensions
    {
        /// <summary>Uses the default Unity Services profile and environment.</summary>
        public static CloudServicesBuilder UseUnityAuthentication(this CloudServicesBuilder builder)
        {
            Guard.NotNull(builder, nameof(builder));
            return builder.UseAuth(new UnityAuthenticationService());
        }

        /// <param name="profile">A local scope for the cached sign-in, to test several players on one machine.</param>
        /// <param name="environment">The Unity Services environment name, for example "production". Null or empty for the default.</param>
        public static CloudServicesBuilder UseUnityAuthentication(this CloudServicesBuilder builder, string profile, string environment = null)
        {
            Guard.NotNull(builder, nameof(builder));
            return builder.UseAuth(new UnityAuthenticationService(profile, environment));
        }

        /// <summary>For full control over <c>UnityServices.InitializeAsync</c>. This overload exposes an SDK type, so only the code that builds the services needs the reference.</summary>
        public static CloudServicesBuilder UseUnityAuthenticationWithOptions(this CloudServicesBuilder builder, InitializationOptions options)
        {
            Guard.NotNull(builder, nameof(builder));
            return builder.UseAuth(new UnityAuthenticationService(options));
        }
    }
}
