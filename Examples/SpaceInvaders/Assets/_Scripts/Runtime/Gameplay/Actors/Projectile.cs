using Pretzel.Pooling;
using UnityEngine;

namespace SpaceInvaders.Gameplay
{
    /// <summary>A pooled bullet or bomb. A kinematic body with a trigger collider; it hits whatever is <see cref="IDamageable"/>.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Projectile : MonoBehaviour, IPoolable, IDamageable
    {
        [SerializeField] private Team _team = Team.Player;
        [SerializeField] private Vector2 _direction = Vector2.up;
        [SerializeField, Min(0f)] private float _speed = 12f;
        [SerializeField] private float _killY = 6.5f;

        private Rigidbody2D _body;
        private PooledObject _pooled;
        private bool _live;

        public Team Team => _team;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _pooled = GetComponent<PooledObject>();
        }

        public void OnTakenFromPool()
        {
            _live = true;
            _body.linearVelocity = _direction.normalized * _speed;
        }

        public void OnReturnedToPool()
        {
            _live = false;
            _body.linearVelocity = Vector2.zero;
        }

        private void Update()
        {
            if (_live && Mathf.Abs(transform.position.y) > _killY)
            {
                Release();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_live && other.TryGetComponent(out IDamageable target) && target.TakeHit(_team))
            {
                Release();
            }
        }

        /// <summary>Projectiles of different teams cancel each other out.</summary>
        public bool TakeHit(Team attacker)
        {
            if (!_live || attacker == _team)
            {
                return false;
            }

            Release();
            return true;
        }

        private void Release()
        {
            _live = false;
            if (_pooled)
            {
                _pooled.Release();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
