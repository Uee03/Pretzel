using System;
using UnityEngine;

namespace Pretzel.Samples.UI.PropertyDrawers
{
    [Serializable]
    public sealed class HealEffect : AbilityEffect
    {
        [Min(0f)] public float amount = 25f;
        public bool overTime;
    }
}
