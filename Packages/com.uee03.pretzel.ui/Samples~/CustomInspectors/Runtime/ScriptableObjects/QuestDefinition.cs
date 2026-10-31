using System.Collections.Generic;
using UnityEngine;

namespace Pretzel.Samples.UI.CustomInspectors
{
    /// <summary>Target of the ScriptableObjects inspector: an asset that is validated and created from a menu.</summary>
    public sealed class QuestDefinition : ScriptableObject
    {
        public string id;
        public string title;
        [TextArea] public string description;
        [Min(0)] public int rewardGold = 50;
        [Min(1)] public int minLevel = 1;
        [Min(1)] public int maxLevel = 10;
        public List<string> objectives = new();
    }
}
