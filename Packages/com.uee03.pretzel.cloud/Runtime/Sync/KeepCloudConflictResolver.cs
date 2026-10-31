namespace Pretzel.Cloud
{
    /// <summary>The cloud copy always wins, so unsynced device changes are discarded. Suits a server-authoritative profile.</summary>
    public sealed class KeepCloudConflictResolver : IConflictResolver
    {
        public ConflictChoice Resolve(ConflictInfo conflict) => ConflictChoice.UseCloud;
    }
}
