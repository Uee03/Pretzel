using System;
using Pretzel.Core;
using Pretzel.Pooling;
using UnityEngine;

namespace Pretzel.Samples.Pooling.Bullets
{
    /// <summary>The pooled implementation of <see cref="IEffectSpawner"/>: one <see cref="IPoolRegistry"/> pool for the impact prefab.</summary>
    public sealed class PooledEffectSpawner : IEffectSpawner, IDisposable
    {
        private readonly IPoolRegistry _registry;
        private readonly ImpactFlash _impactPrefab;

        public PooledEffectSpawner(IPoolRegistry registry, ImpactFlash impactPrefab, int prewarm)
        {
            _registry = Guard.NotNull(registry, nameof(registry));
            _impactPrefab = Guard.NotDestroyed(impactPrefab, nameof(impactPrefab));
            _registry.Prewarm(impactPrefab, prewarm);
        }

        public IComponentPool<ImpactFlash> Pool => _registry.GetPool(_impactPrefab);

        public void SpawnImpact(Vector3 position, Vector3 normal)
        {
            Quaternion rotation = normal.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(normal) : Quaternion.identity;
            _registry.Spawn(_impactPrefab, position + normal * 0.02f, rotation);
        }

        public void Dispose()
        {
            _registry.Dispose();
        }
    }
}
