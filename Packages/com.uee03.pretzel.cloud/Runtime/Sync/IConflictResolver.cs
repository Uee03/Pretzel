namespace Pretzel.Cloud
{
    /// <summary>
    /// Decides which copy wins when the device and the cloud both changed a key since they last agreed.
    /// Called on the main thread, once per conflicting key, and must not block.
    /// </summary>
    public interface IConflictResolver
    {
        ConflictChoice Resolve(ConflictInfo conflict);
    }
}
