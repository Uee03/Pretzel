using System;
using UnityEngine.Scripting;

namespace Pretzel.Samples.Data
{
    [Serializable, Preserve]
    public sealed class ItemReward : IQuestReward
    {
        public string itemId;
        public int count = 1;

        public string Describe() => count + " x " + itemId;
    }
}
