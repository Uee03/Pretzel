using UnityEngine;

namespace Pretzel.Samples.Pooling.Bullets
{
    /// <summary>
    /// What gameplay code needs from the effects system. Bullets depend on this, not on pooling, so tests can pass a fake and the
    /// pooling strategy can change without touching gameplay.
    /// </summary>
    public interface IEffectSpawner
    {
        void SpawnImpact(Vector3 position, Vector3 normal);
    }
}
