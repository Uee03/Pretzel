namespace Pretzel.Multiplayer
{
    /// <summary>Failure categories exposed to callers. Backend (UGS, NGO) specifics never leak past this enum.</summary>
    public enum MultiplayerErrorCode
    {
        Unknown = 0,
        InvalidRequest = 1,
        NotSignedIn = 2,
        ServicesUnavailable = 3,
        NetworkManagerMissing = 4,
        ConnectionFailed = 5,
        SessionNotFound = 6,
        SessionFull = 7,
        SessionEnded = 8,
        Kicked = 9,
        NotAuthorized = 10,
        RateLimited = 11,
        VersionMismatch = 12,
        PlatformRestricted = 13,
        InvalidOperation = 14,
        ConnectionLost = 15,
    }
}
