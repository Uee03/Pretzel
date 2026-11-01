using Pretzel.Core.Timers;
using Pretzel.Multiplayer;
using SpaceInvaders.Gameplay;
using Unity.Netcode;
using UnityEngine;

namespace SpaceInvaders.Coop
{
    /// <summary>
    /// One player's cannon in a co-op match. The controlling machine reads input and moves it (owner authority, replicated by
    /// NetworkTransform); the host validates every shot, decides every hit and owns the lives. Its colour follows the slot the host assigned.
    /// </summary>
    [RequireComponent(typeof(NetworkPlayerIdentity))]
    [DisallowMultipleComponent]
    public sealed class CoopShip : NetworkBehaviour, IDamageable
    {
        private static readonly Color[] _palette =
        {
            new(0.4f, 1f, 0.5f),
            new(0.35f, 0.8f, 1f),
            new(1f, 0.8f, 0.3f),
            new(1f, 0.5f, 0.8f)
        };

        private static readonly ICoopShipInput _sharedInput = new CoopShipInput();

        private readonly NetworkVariable<bool> _alive = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<bool> _invulnerable = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        [SerializeField] private float _speed = 8f;
        [SerializeField] private float _limitX = 7.3f;
        [SerializeField, Min(0.05f)] private float _fireIntervalSeconds = 0.45f;
        [SerializeField, Min(0f)] private float _invulnerableSeconds = 2f;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private SpriteRenderer[] _renderers = new SpriteRenderer[0];

        private readonly CountdownTimer _invulnerableTimer = new(1f);
        private Cooldown _ownerCooldown;
        private Cooldown _serverCooldown;
        private Color[] _baseColors;
        private NetworkPlayerIdentity _identity;
        private CoopMatch _match;
        private bool _wasLocal;
        private bool _announced;
        private bool _slotKnown;
        private bool _dimmed;

        /// <summary>Overrides the keyboard and gamepad, for tests.</summary>
        public ICoopShipInput Input { get; set; }

        public bool IsSlotKnown => _identity != null && _identity.ControllerClientId != NetworkPlayerIdentity.NoController;

        /// <summary>The host gave this ship a slot at some point. Unlike <see cref="IsSlotKnown"/> it stays true while the player is disconnected.</summary>
        public bool HasSlot => _slotKnown;

        public int Slot => _identity != null ? _identity.SlotIndex : -1;

        public bool IsLocallyControlled => _identity != null && _identity.IsLocallyControlled;

        public bool IsAlive => _alive.Value;

        public bool IsInvulnerable => _invulnerable.Value;

        private void Awake()
        {
            _identity = GetComponent<NetworkPlayerIdentity>();
            _ownerCooldown = new Cooldown(_fireIntervalSeconds);
            // The host accepts a shot a little sooner than the owner sends it, so network jitter never swallows one.
            _serverCooldown = new Cooldown(_fireIntervalSeconds * 0.9f);
            _baseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _baseColors[i] = _renderers[i].color;
            }

            _invulnerableTimer.Completed += OnInvulnerabilityEnded;
        }

        public override void OnDestroy()
        {
            _invulnerableTimer.Completed -= OnInvulnerabilityEnded;
            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            _match = FindAnyObjectByType<CoopMatch>();
            if (_match == null)
            {
                Debug.LogError("[Coop] A CoopShip spawned in a scene without a CoopMatch.", this);
                enabled = false;
                return;
            }

            _match.RegisterShip(this);
            _identity.IdentityChanged += OnIdentityChanged;
            _alive.OnValueChanged += OnFlagChanged;
            _invulnerable.OnValueChanged += OnFlagChanged;
            _match.PhaseChanged += OnPhaseChanged;
            OnIdentityChanged(_identity);
            RefreshVisibility();
        }

        public override void OnNetworkDespawn()
        {
            _identity.IdentityChanged -= OnIdentityChanged;
            _alive.OnValueChanged -= OnFlagChanged;
            _invulnerable.OnValueChanged -= OnFlagChanged;
            if (_match != null)
            {
                _match.PhaseChanged -= OnPhaseChanged;
                _match.UnregisterShip(this);
                CountLocalShips();
            }

            // Cleared last: ShipRemoved listeners still need to know which slot this ship held.
            _announced = false;
            _wasLocal = false;
            _slotKnown = false;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            _ownerCooldown.Tick(deltaTime);
            if (IsServer)
            {
                _serverCooldown.Tick(deltaTime);
                _invulnerableTimer.Tick(deltaTime);
            }

            UpdateBlink();
            if (!IsSpawned || _match == null || !_identity.IsLocallyControlled || !_alive.Value || _match.Phase != CoopPhase.Playing)
            {
                return;
            }

            ICoopShipInput source = Input ?? _sharedInput;
            bool everyScheme = _match.LocalShipCount <= 1;
            int index = _identity.LocalPlayerIndex;
            Vector3 position = transform.position;
            position.x = Mathf.Clamp(position.x + source.ReadHorizontal(index, everyScheme) * _speed * deltaTime, -_limitX, _limitX);
            transform.position = position;
            if (source.ReadFire(index, everyScheme) && _ownerCooldown.TryUse())
            {
                FireRpc();
            }
        }

        /// <summary>Called by a bomb on the host. Returns true when the ship was hit.</summary>
        public bool TakeHit(Team attacker)
        {
            if (!IsServer || !IsSpawned || attacker != Team.Invaders || !_alive.Value || _invulnerable.Value || _match.Phase != CoopPhase.Playing)
            {
                return false;
            }

            _invulnerable.Value = true;
            _invulnerableTimer.Reset(_invulnerableSeconds);
            _invulnerableTimer.Start();
            _match.Bus.Publish(new PlayerHit(transform.position));
            _match.ServerReportHit(this);
            return true;
        }

        /// <summary>Host only: the last life is gone.</summary>
        public void ServerEliminate()
        {
            if (IsServer)
            {
                _alive.Value = false;
                _invulnerable.Value = false;
                _invulnerableTimer.Reset();
            }
        }

        /// <summary>Host only: back in the game with a fresh start, for the first wave of a new match.</summary>
        public void ServerRevive()
        {
            if (!IsServer)
            {
                return;
            }

            _alive.Value = true;
            _invulnerable.Value = false;
            _invulnerableTimer.Reset();
            _serverCooldown.Reset();
        }

        // Only the controlling machine may ask, and the host re-checks everything it can: the match runs, the ship lives, the cooldown is over.
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void FireRpc()
        {
            if (!_alive.Value || _match.Phase != CoopPhase.Playing || !_serverCooldown.TryUse())
            {
                return;
            }

            Vector3 origin = _muzzle ? _muzzle.position : transform.position;
            origin.x = Mathf.Clamp(origin.x, -_limitX, _limitX);
            if (_match.Projectiles.SpawnFor(ProjectileKind.Bullet, origin, Slot) != null)
            {
                _match.Bus.Publish(new PlayerFired());
            }
        }

        private void OnIdentityChanged(NetworkPlayerIdentity identity)
        {
            if (IsSlotKnown)
            {
                _slotKnown = true;
            }

            ApplyColor(identity.SlotIndex);
            bool local = identity.IsLocallyControlled;
            if (local != _wasLocal)
            {
                _wasLocal = local;
                CountLocalShips();
            }

            if (IsServer && !_announced && IsSlotKnown)
            {
                _announced = true;
                _match.ServerAnnounceShip(this);
            }
        }

        private void OnInvulnerabilityEnded()
        {
            if (IsServer && IsSpawned)
            {
                _invulnerable.Value = false;
            }
        }

        private void OnFlagChanged(bool previous, bool current)
        {
            RefreshVisibility();
        }

        private void OnPhaseChanged(CoopPhase phase)
        {
            RefreshVisibility();
        }

        private void CountLocalShips()
        {
            int count = 0;
            foreach (CoopShip ship in _match.Ships)
            {
                if (ship != null && ship.IsLocallyControlled)
                {
                    count++;
                }
            }

            _match.SetLocalShipCount(count);
        }

        // Ships stay spawned between matches, so they are hidden in the lobby and once their lives are gone.
        private void RefreshVisibility()
        {
            bool visible = _match != null && _match.Phase != CoopPhase.Lobby && _alive.Value;
            foreach (SpriteRenderer spriteRenderer in _renderers)
            {
                spriteRenderer.enabled = visible;
            }
        }

        private void ApplyColor(int slot)
        {
            Color tint = _palette[Mathf.Abs(slot) % _palette.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                tint.a = _baseColors[i].a;
                _baseColors[i] = tint;
                _renderers[i].color = tint;
            }
        }

        private void UpdateBlink()
        {
            bool shouldDim = _invulnerable.Value && Mathf.Sin(Time.time * 40f) > 0f;
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
    }
}
