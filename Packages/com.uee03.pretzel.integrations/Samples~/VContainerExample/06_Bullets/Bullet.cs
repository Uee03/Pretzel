using UnityEngine;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>A pooled bullet: only data and a Transform, no MonoBehaviour and no Update.</summary>
    public sealed class Bullet
    {
        public Bullet(Transform transform) => Transform = transform;

        public Transform Transform { get; }

        public Vector3 Velocity { get; set; }

        public float Age { get; set; }
    }
}
