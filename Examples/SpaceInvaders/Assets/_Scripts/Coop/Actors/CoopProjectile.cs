using Pretzel.Pooling;
using SpaceInvaders.Gameplay;
using Unity.Netcode;
using UnityEngine;

namespace SpaceInvaders.Coop
{
    /// <summary>
    /// A bullet or a bomb in a co-op match, spawned from a pool on every machine. It flies in a straight line, so each machine moves it
    /// from the spawn position and nothing is sent while it flies. Only the host sees hits: other machines switch the collider off.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class CoopProjectile : NetworkBehaviour, IPoolable, IDamageable
    {
        [SerializeField] private Team _team = Team.Player;
        [SerializeField] private Vector2 _direction = Vector2.up;
        [SerializeField, Min(0f)] private float _speed = 12f;
        [SerializeField] private float _killY = 6.5f;

        private Rigidbody2D _body;
        private Collider2D _collider;
        private CoopProjectileSpawner _spawner;

        public Team Team => _team;

        /// <summary>The slot of the player who fired it, or -1 for a bomb. Host only.</summary>
        public int OwnerSlot { get; private set; } = -1;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
        }

        public void OnTakenFromPool()
        {
            _body.linearVelocity = _direction.normalized * _speed;
        }

        public void OnReturnedToPool()
        {
            _body.linearVelocity = Vector2.zero;
            OwnerSlot = -1;
            _spawner = null;
        }

        public override void OnNetworkSpawn()
        {
            _collider.enabled = IsServer;
            _body.linearVelocity = _direction.normalized * _speed;
        }

        /// <summary>Host only: remembers who fired it and where to report whose bullet is hitting.</summary>
        public void ServerBind(CoopProjectileSpawner spawner, int ownerSlot)
        {
            _spawner = spawner;
            OwnerSlot = ownerSlot;
        }

        private void Update()
        {
            if (IsServer && IsSpawned && Mathf.Abs(transform.position.y) > _killY)
            {
                Despawn();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer || !IsSpawned || !other.TryGetComponent(out IDamageable target))
            {
                return;
            }

            // The hit and the events it causes run synchronously, so the score reaction can read whose bullet it was.
            if (_spawner != null)
            {
                _spawner.CurrentShooterSlot = _team == Team.Player ? OwnerSlot : -1;
            }

            bool hit = target.TakeHit(_team);
            if (_spawner != null)
            {
                _spawner.CurrentShooterSlot = -1;
            }

            if (hit)
            {
                Despawn();
            }
        }

        /// <summary>Projectiles of different teams cancel each other out.</summary>
        public bool TakeHit(Team attacker)
        {
            if (!IsServer || !IsSpawned || attacker == _team)
            {
                return false;
            }

            Despawn();
            return true;
        }

        private void Despawn()
        {
            if (NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
        }
    }
}
