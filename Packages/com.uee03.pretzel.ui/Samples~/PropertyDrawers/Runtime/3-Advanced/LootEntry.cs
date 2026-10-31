using System;
using UnityEngine;

namespace Pretzel.Samples.UI.PropertyDrawers
{
    /// <summary>A serializable type used in a list. Its drawer picks the item id from the catalog and validates the rest.</summary>
    [Serializable]
    public sealed class LootEntry
    {
        public string itemId;
        [Min(1)] public int minCount = 1;
        [Min(1)] public int maxCount = 3;
        [Min(0f)] public float weight = 1f;
    }
}
