using System;
using UnityEngine;

namespace Pretzel.Samples.UI.PropertyDrawers
{
    /// <summary>A serializable class drawn by a type drawer: child fields laid out in rows.</summary>
    [Serializable]
    public sealed class SpawnSettings
    {
        public string label = "Wave";
        [Min(1)] public int count = 5;
        [Min(0f)] public float interval = 1f;
        public bool randomize;
    }
}
