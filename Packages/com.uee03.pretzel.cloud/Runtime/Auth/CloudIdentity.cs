using Pretzel.Core;

namespace Pretzel.Cloud
{
    /// <summary>A platform credential used to sign in or link an account. Never log it or put it in source control.</summary>
    public sealed class CloudIdentity
    {
        public CloudIdentityProvider Provider { get; }

        /// <summary>The main credential. What it is depends on <see cref="Provider"/>.</summary>
        public string Token { get; }

        /// <summary>The second value some providers need (Steam identity, Oculus user id, OpenID Connect provider name), or null.</summary>
        public string Secondary { get; }

        public CloudIdentity(CloudIdentityProvider provider, string token, string secondary = null)
        {
            Provider = provider;
            Token = Guard.NotNullOrEmpty(token, nameof(token));
            Secondary = secondary;
        }

        // The token is a secret, so it is left out on purpose.
        public override string ToString() => Provider.ToString();
    }
}
