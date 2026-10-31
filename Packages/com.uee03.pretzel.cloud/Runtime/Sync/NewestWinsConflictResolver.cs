using Pretzel.Core;

namespace Pretzel.Cloud
{
    /// <summary>The copy with the later save time wins. A tie keeps the device copy. This is the default policy.</summary>
    public sealed class NewestWinsConflictResolver : IConflictResolver
    {
        public ConflictChoice Resolve(ConflictInfo conflict)
        {
            Guard.NotNull(conflict, nameof(conflict));
            return conflict.CloudIsNewer ? ConflictChoice.UseCloud : ConflictChoice.UseLocal;
        }
    }
}
