using System;
using Pretzel.Data;

namespace Pretzel.Cloud.Tests
{
    /// <summary>A save type with schema version 2, to test cloud payload migration.</summary>
    [Serializable]
    [SchemaVersion(2)]
    public sealed class TestVersionedProfile
    {
        public string displayName;
        public int level;
    }
}
