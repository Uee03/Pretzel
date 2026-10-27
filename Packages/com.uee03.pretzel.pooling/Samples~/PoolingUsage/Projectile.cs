using Pretzel.Pooling;
using UnityEngine;

namespace Pretzel.Samples.Pooling
{
    /// <summary>Moves forward and returns itself to its pool after a lifetime. Resets its state in the pool hooks.</summary>
    public sealed class Projectile : MonoBehaviour, IPoolable
    {
        [SerializeField] private float _speed = 12f;
        [SerializeField] private float _lifetime = 2f;

        private PooledObject _pooled;
        private float _travelled;

        private void Awake()
        {
            _pooled = GetComponent<PooledObject>();
        }

        private void Update()
        {
            float step = _speed * Time.deltaTime;
            transform.position += transform.forward * step;
            _travelled += step;
        }

        public void OnTakenFromPool()
        {
            _travelled = 0f;
            _pooled.ReleaseAfter(_lifetime);
        }

        public void OnReturnedToPool()
        {
            Debug.Log($"{name} returned after {_travelled:0.0} m.", this);
        }
    }
}
