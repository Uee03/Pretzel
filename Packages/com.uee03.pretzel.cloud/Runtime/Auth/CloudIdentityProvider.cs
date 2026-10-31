namespace Pretzel.Cloud
{
    /// <summary>Platform identity providers a player can sign in with or link. The token each one needs comes from the platform SDK.</summary>
    public enum CloudIdentityProvider
    {
        /// <summary><see cref="CloudIdentity.Token"/> is the Apple ID token.</summary>
        Apple = 0,

        /// <summary><see cref="CloudIdentity.Token"/> is the Google ID token.</summary>
        Google = 1,

        /// <summary><see cref="CloudIdentity.Token"/> is the Google Play Games server auth code.</summary>
        GooglePlayGames = 2,

        /// <summary><see cref="CloudIdentity.Token"/> is the Facebook access token.</summary>
        Facebook = 3,

        /// <summary><see cref="CloudIdentity.Token"/> is the session ticket and <see cref="CloudIdentity.Secondary"/> the service identity.</summary>
        Steam = 4,

        /// <summary><see cref="CloudIdentity.Token"/> is the nonce and <see cref="CloudIdentity.Secondary"/> the Oculus user id.</summary>
        Oculus = 5,

        /// <summary><see cref="CloudIdentity.Token"/> is the Unity Player Accounts access token.</summary>
        Unity = 6,

        /// <summary><see cref="CloudIdentity.Token"/> is the ID token and <see cref="CloudIdentity.Secondary"/> the OpenID Connect provider name.</summary>
        OpenIdConnect = 7
    }
}
