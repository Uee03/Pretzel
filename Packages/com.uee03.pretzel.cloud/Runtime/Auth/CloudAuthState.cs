namespace Pretzel.Cloud
{
    public enum CloudAuthState
    {
        SignedOut = 0,
        SigningIn = 1,
        SignedIn = 2,

        /// <summary>The session token expired. Sign in again.</summary>
        Expired = 3
    }
}
