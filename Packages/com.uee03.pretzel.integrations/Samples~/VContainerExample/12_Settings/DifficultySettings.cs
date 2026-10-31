using System;
using UnityEngine;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Configuration as a serializable class, edited on the scope and registered as an instance.</summary>
    [Serializable]
    public sealed class DifficultySettings
    {
        [Min(1)] public int enemyHealth = 10;
        [Min(0f)] public float damageMultiplier = 1f;
    }
}
