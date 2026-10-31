using System;

namespace Pretzel.Cloud
{
    /// <summary>
    /// The text stored in one Cloud Save item: the serialized value plus what is needed to compare it with the device copy.
    /// Public fields are the stored keys of cloud data, so keep their names.
    /// </summary>
    [Serializable]
    public sealed class CloudPayload
    {
        public const int CurrentFormat = 1;

        public int format = CurrentFormat;

        /// <summary>When the device saved the value, in UTC ticks.</summary>
        public long savedAtUtcTicks;

        /// <summary>The <c>SchemaVersionAttribute</c> version of the saved type, or 0 when it has none.</summary>
        public int schemaVersion;

        /// <summary>The value as written by the Pretzel Data serializer.</summary>
        public string data = string.Empty;
    }
}
