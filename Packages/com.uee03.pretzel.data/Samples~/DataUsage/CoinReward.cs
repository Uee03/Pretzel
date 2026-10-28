using System;
using UnityEngine.Scripting;

namespace Pretzel.Samples.Data
{
    [Serializable, Preserve]
    public sealed class CoinReward : IQuestReward
    {
        public int amount;

        public string Describe() => amount + " coins";
    }
}
