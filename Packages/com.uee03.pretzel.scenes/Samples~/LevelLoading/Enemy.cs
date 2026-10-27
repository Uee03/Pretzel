using Pretzel.Pooling;
using UnityEngine;

namespace Pretzel.Samples.LevelLoading
{
    /// <summary>A pooled enemy that bobs in place. Resets its state each time it is taken from the pool.</summary>
    public sealed class Enemy : MonoBehaviour, IPoolable
    {
        [SerializeField] private float _bobHeight = 0.25f;
        [SerializeField] private float _bobSpeed = 2f;

        private Vector3 _origin;
        private float _phase;

        private void Update()
        {
            transform.position = _origin + Vector3.up * (Mathf.Sin(Time.time * _bobSpeed + _phase) * _bobHeight);
        }

        public void OnTakenFromPool()
        {
            _origin = transform.position;
            _phase = Random.Range(0f, Mathf.PI * 2f);
        }

        public void OnReturnedToPool()
        {
        }
    }
}
