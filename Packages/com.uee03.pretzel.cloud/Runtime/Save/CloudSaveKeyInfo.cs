using System;

namespace Pretzel.Cloud
{
    /// <summary>A key in player data, without its value.</summary>
    public sealed class CloudSaveKeyInfo
    {
        public string Key { get; }

        public string WriteLock { get; }

        public DateTime? ModifiedUtc { get; }

        public CloudSaveKeyInfo(string key, string writeLock, DateTime? modifiedUtc = null)
        {
            Key = key;
            WriteLock = writeLock;
            ModifiedUtc = modifiedUtc;
        }
    }
}
