using System;

namespace Pretzel.Data
{
    /// <summary>
    /// Declares the current schema version of a saved type. The store then wraps the data in an envelope
    /// that records the version, and runs registered <see cref="IMigration"/> steps when it loads older data.
    /// Types without this attribute are saved as plain serializer output.
    /// Versioned types need a JSON serializer, because the data is nested inside a JSON envelope.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class SchemaVersionAttribute : Attribute
    {
        public int Version { get; }

        public SchemaVersionAttribute(int version)
        {
            if (version < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(version), version, "Version must be 1 or greater.");
            }

            Version = version;
        }
    }
}
