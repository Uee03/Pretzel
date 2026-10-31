using System;

namespace Pretzel.Cloud.Tests
{
    /// <summary>A small save type for the sync tests.</summary>
    [Serializable]
    public sealed class TestProfile
    {
        public string name;
        public int level;

        public TestProfile()
        {
        }

        public TestProfile(string name, int level)
        {
            this.name = name;
            this.level = level;
        }
    }
}
