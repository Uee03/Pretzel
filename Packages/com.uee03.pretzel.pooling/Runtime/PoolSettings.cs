using System;
using UnityEngine;

namespace Pretzel.Pooling
{
    /// <summary>Inspector-editable settings for a <see cref="ComponentPool{T}"/>.</summary>
    [Serializable]
    public sealed class PoolSettings
    {
        [Tooltip("Throws on a double release. Costs a lookup per release, so turn it off once stable.")]
        public bool collectionCheck = true;

        [Min(1)] public int defaultCapacity = 10;

        [Tooltip("Instances released beyond this many idle ones are destroyed.")]
        [Min(1)] public int maxSize = 1000;
    }
}
