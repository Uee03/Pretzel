using System;

namespace Pretzel.Samples.Cloud.SaveUsage
{
    /// <summary>The save data of the sample. Any serializable class works; the synced store stores what the Pretzel Data serializer writes.</summary>
    [Serializable]
    public sealed class PlayerProfile
    {
        public string displayName = "Player";
        public int level = 1;
        public int coins;
    }
}
