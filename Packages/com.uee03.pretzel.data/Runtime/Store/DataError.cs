namespace Pretzel.Data
{
    /// <summary>Actionable reason for an unsuccessful load. Authentication failure cannot distinguish damage from a wrong key.</summary>
    public enum DataError
    {
        None = 0,
        InvalidData = 1,
        MissingKey = 2,
        AuthenticationFailed = 3,
        UnsupportedFormat = 4,
        SchemaMismatch = 5,
        IoFailure = 6
    }
}
