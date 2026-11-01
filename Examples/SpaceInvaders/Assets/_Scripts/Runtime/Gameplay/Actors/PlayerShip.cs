using Pretzel.Core;
using Pretzel.Core.Timers;
using Pretzel.Events;
using UnityEngine;
using VContainer;

namespace SpaceInvaders.Gameplay
{
    /// <summary>The player's cannon. Reads input, fires through the projectile pool and reports hits on the event bus.</summary>
    public sealed class PlayerShip : MonoBehaviour, IDamageable
    {
        [SerializeField] private float _speed = 8f;
        [SerializeField] private float _limitX = 7.3f;
        [SerializeField, Min(0.05f)] private float _fireIntervalSeconds = 0.45f;
        [SerializeField, Min(0f)] private float _invulnerableSeconds = 2f;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private SpriteRenderer[] _renderers = new SpriteRenderer[0];

        private readonly CountdownTimer _invulnerable = new(1f);
        private Cooldown _cooldown;
        private Color[] _baseColors;
        private IEventBus _bus;
        private IProjectileSpawner _projectiles;
        private ShipControls _controls;
        private bool _dimmed;

        public bool IsAlive { get; private set; } = true;

        public bool IsInvulnerable => _invulnerable.IsRunning;

        /// <summary>Set by the dev console. Hits are ignored while on.</summary>
        public bool GodMode { get; set; }

        // Runs when the level scope builds, before Awake: keep it to storing dependencies.
        [Inject]
        public void Construct(IEventBus bus, IProjectileSpawner projectiles, ShipControls controls)
        {
            _bus = Guard.NotNull(bus, nameof(bus));
            _projectiles = Guard.NotNull(projectiles, nameof(projectiles));
            _controls = Guard.NotNull(controls, nameof(controls));
        }

        private void Awake()
        {
            _cooldown = new Cooldown(_fireIntervalSeconds);
            _invulnerable.Reset(_invulnerableSeconds);
            _baseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _baseColors[i] = _renderers[i].color;
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            _cooldown.Tick(deltaTime);
            _invulnerable.Tick(deltaTime);
            UpdateBlink();
            if (!IsAlive || _controls == null)
            {
                return;
            }

            Vector3 position = transform.position;
            position.x = Mathf.Clamp(position.x + _controls.Horizontal * _speed * deltaTime, -_limitX, _limitX);
            transform.position = position;
            if (_controls.FireHeld)
            {
                TryFire();
            }
        }

        public bool TryFire()
        {
            if (!IsAlive || !_cooldown.TryUse())
            {
                return false;
            }

            _projectiles.Spawn(ProjectileKind.Bullet, _muzzle ? _muzzle.position : transform.position);
            _bus.Publish(new PlayerFired());
            return true;
        }

        public bool TakeHit(Team attacker)
        {
            if (attacker != Team.Invaders || !IsAlive || GodMode || _invulnerable.IsRunning)
            {
                return false;
            }

            _invulnerable.Start();
            _bus.Publish(new PlayerHit(transform.position));
            return true;
        }

        /// <summary>The last life is gone: hide the ship and stop reading input.</summary>
        public void Eliminate()
        {
            IsAlive = false;
            _invulnerable.Reset();
            SetRenderersEnabled(false);
        }

        private void UpdateBlink()
        {
            bool shouldDim = _invulnerable.IsRunning && Mathf.Sin(Time.time * 40f) > 0f;
            if (shouldDim == _dimmed)
            {
                return;
            }

            _dimmed = shouldDim;
            for (int i = 0; i < _renderers.Length; i++)
            {
                Color color = _baseColors[i];
                color.a = shouldDim ? 0.25f : color.a;
                _renderers[i].color = color;
            }
        }

        private void SetRenderersEnabled(bool visible)
        {
            foreach (SpriteRenderer spriteRenderer in _renderers)
            {
                spriteRenderer.enabled = visible;
            }
        }
    }
}
