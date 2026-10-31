namespace Pretzel.Cloud
{
    public enum ConflictChoice
    {
        /// <summary>Keep the device copy and upload it over the cloud copy.</summary>
        UseLocal = 0,

        /// <summary>Replace the device copy with the cloud copy.</summary>
        UseCloud = 1
    }
}
