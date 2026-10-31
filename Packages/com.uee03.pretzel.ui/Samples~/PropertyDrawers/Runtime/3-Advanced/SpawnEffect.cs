using System;
using UnityEngine;

namespace Pretzel.Samples.UI.PropertyDrawers
{
    [Serializable]
    public sealed class SpawnEffect : AbilityEffect
    {
        public GameObject prefab;
        [Min(1)] public int count = 1;
    }
}
