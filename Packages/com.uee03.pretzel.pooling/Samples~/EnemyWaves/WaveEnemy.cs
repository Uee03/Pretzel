using System;
using Pretzel.Pooling;
using UnityEngine;

namespace Pretzel.Samples.Pooling.Waves
{
    /// <summary>
    /// An enemy that walks to the centre. It resets everything a previous life changed when it is taken from the pool, and
    /// tells the spawner when it goes back, so the spawner never keeps a stale reference.
    /// </summary>
    public sealed class WaveEnemy : MonoBehaviour, IPoolable
    {
        private const float ArriveDistance = 0.8f;

        private PooledObject _pooled;
        private Action<WaveEnemy, bool> _returned;
        private float _speed;
        private float _health;
        private bool _arrived;

        public EnemyKind Kind { get; private set; }

        public float Health => _health;

        private void Awake()
        {
            _pooled = GetComponent<PooledObject>();
        }

        /// <summary>Sets the per-spawn values. Called right after the instance is taken.</summary>
        public void Configure(EnemyKind kind, float speed, float health, Action<WaveEnemy, bool> returned)
        {
            Kind = kind;
            _speed = speed;
            _health = health;
            _returned = returned;
        }

        public void Hit(float damage)
        {
            _health -= damage;
            if (_health <= 0f)
            {
                _pooled.Release();
            }
        }

        private void Update()
        {
            Vector3 toCentre = -transform.position;
            toCentre.y = 0f;
            float distance = toCentre.magnitude;
            if (distance <= ArriveDistance)
            {
                _arrived = true;
                _pooled.Release();
                return;
            }

            transform.position += toCentre / distance * (_speed * Time.deltaTime);
        }

        public void OnTakenFromPool()
        {
            _arrived = false;
        }

        public void OnReturnedToPool()
        {
            Action<WaveEnemy, bool> returned = _returned;
            _returned = null;
            returned?.Invoke(this, _arrived);
        }
    }
}
