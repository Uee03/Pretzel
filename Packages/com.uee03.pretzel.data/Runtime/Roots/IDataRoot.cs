namespace Pretzel.Data
{
    /// <summary>
    /// Supplies the base folder of a <see cref="IDataStore"/>. Resolved lazily on first use, on the main thread.
    /// </summary>
    public interface IDataRoot
    {
        string FullPath { get; }
    }
}
