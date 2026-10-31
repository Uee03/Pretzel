using System;

namespace Pretzel.Cloud
{
    /// <summary>One player data value read from the cloud, with the write lock needed to update it safely.</summary>
    public sealed class CloudSaveItem
    {
        public string Key { get; }

        /// <summary>The stored text.</summary>
        public string Value { get; }

        /// <summary>Changes on every write. Pass it back to <see cref="ICloudSaveService.SaveAsync"/> to fail with <see cref="CloudErrorKind.Conflict"/> when someone else wrote in between.</summary>
        public string WriteLock { get; }

        /// <summary>The server's last modification time in UTC, when it reported one.</summary>
        public DateTime? ModifiedUtc { get; }

        public CloudSaveItem(string key, string value, string writeLock, DateTime? modifiedUtc = null)
        {
            Key = key;
            Value = value;
            WriteLock = writeLock;
            ModifiedUtc = modifiedUtc;
        }
    }
}
