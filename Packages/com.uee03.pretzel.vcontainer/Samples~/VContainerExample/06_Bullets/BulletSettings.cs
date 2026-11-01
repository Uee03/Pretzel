using System;
using UnityEngine;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Bullet tuning, edited on the scope and registered as an instance.</summary>
    [Serializable]
    public sealed class BulletSettings
    {
        [Min(0.1f)] public float speed = 6f;
        [Min(0.1f)] public float lifetime = 2f;
        [Min(0.01f)] public float fireInterval = 0.05f;
        [Min(0.01f)] public float size = 0.15f;
        [Min(1)] public int poolCapacity = 64;
    }
}
