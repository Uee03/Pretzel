using Pretzel.Pooling;
using UnityEngine;

namespace Pretzel.Samples.Pooling.Bullets
{
    /// <summary>A short-lived hit effect: a flash that grows and shrinks, then returns itself with <see cref="PooledObject.ReleaseAfter"/>.</summary>
    public sealed class ImpactFlash : MonoBehaviour, IPoolable
    {
        [SerializeField, Min(0.05f)] private float _duration = 0.25f;
        [SerializeField, Min(0.05f)] private float _size = 0.8f;

        private PooledObject _pooled;
        private float _age;

        private void Awake()
        {
            _pooled = GetComponent<PooledObject>();
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / _duration);
            // Grows quickly, then shrinks to nothing.
            float scale = _size * Mathf.Sin(t * Mathf.PI);
            transform.localScale = Vector3.one * scale;
        }

        public void OnTakenFromPool()
        {
            _age = 0f;
            transform.localScale = Vector3.zero;
            _pooled.ReleaseAfter(_duration);
        }

        public void OnReturnedToPool()
        {
        }
    }
}
