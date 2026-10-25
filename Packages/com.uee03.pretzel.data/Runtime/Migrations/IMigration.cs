namespace Pretzel.Data
{
    /// <summary>Upgrades the serialized payload of one saved type from one schema version to a later one.</summary>
    public interface IMigration
    {
        int FromVersion { get; }

        int ToVersion { get; }

        /// <summary>Receives the serializer output of <see cref="FromVersion"/> and returns the text for <see cref="ToVersion"/>.</summary>
        string Migrate(string payload);
    }
}
