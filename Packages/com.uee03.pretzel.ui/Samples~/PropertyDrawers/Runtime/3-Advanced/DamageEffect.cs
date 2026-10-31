using System;
using UnityEngine;

namespace Pretzel.Samples.UI.PropertyDrawers
{
    [Serializable]
    public sealed class DamageEffect : AbilityEffect
    {
        [Min(0f)] public float amount = 10f;
        public string element = "Fire";
    }
}
