namespace Pretzel.Cloud
{
    /// <summary>The device copy always wins, so the cloud copy is overwritten. Suits a single-device game or a first upload of existing progress.</summary>
    public sealed class KeepLocalConflictResolver : IConflictResolver
    {
        public ConflictChoice Resolve(ConflictInfo conflict) => ConflictChoice.UseLocal;
    }
}
