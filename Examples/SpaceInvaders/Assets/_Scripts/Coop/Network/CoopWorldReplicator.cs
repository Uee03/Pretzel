using Pretzel.Core;
using Pretzel.Events;
using SpaceInvaders.Gameplay;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace SpaceInvaders.Coop
{
    /// <summary>
    /// Shows every machine the host's world. The host runs the invaders, the mystery ship and the bunkers; this component copies their
    /// state into compact replicated values (one snapshot per march step or kill for the formation, not 55 transforms), and clients
    /// apply them to the same scene objects with their simulation switched off.
    /// </summary>
    public sealed class CoopWorldReplicator : NetworkBehaviour
    {
        private const float BunkerPollSeconds = 0.1f;

        private readonly NetworkVariable<FormationSnapshot> _formationState = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<UfoSnapshot> _ufoState = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkList<byte> _bunkerState = new(null, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        [SerializeField] private InvaderFormation _formation;
        [SerializeField] private Ufo _ufo;
        [SerializeField] private BunkerField _bunkers;

        private CompositeDisposable _subscriptions = new();
        private IEventBus _bus;
        private WaveSet _waves;
        private byte[] _healthBuffer = new byte[0];
        private int _generation;
        private int _publishedWave;
        private int _appliedGeneration = -1;
        private bool _mirroring;
        private float _bunkerTimer;

        // Runs when the level scope builds, before Awake: keep it to storing dependencies.
        [Inject]
        public void Construct(IEventBus bus, WaveSet waves)
        {
            _bus = Guard.NotNull(bus, nameof(bus));
            _waves = Guard.NotNull(waves, nameof(waves));
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // This machine may have mirrored a host earlier in the same scene.
                _formation.SimulationEnabled = true;
                SpawnServerSide();
                return;
            }

            _mirroring = true;
            _formation.SimulationEnabled = false;
            _formationState.OnValueChanged += OnFormationChanged;
            _ufoState.OnValueChanged += OnUfoChanged;
            _bunkerState.OnListChanged += OnBunkersChanged;
            ApplyFormation(_formationState.Value);
            ApplyUfo(_ufoState.Value);
            ApplyBunkers();
        }

        public override void OnNetworkDespawn()
        {
            _formationState.OnValueChanged -= OnFormationChanged;
            _ufoState.OnValueChanged -= OnUfoChanged;
            _bunkerState.OnListChanged -= OnBunkersChanged;
            _subscriptions.Dispose();
            _subscriptions = new CompositeDisposable();
            if (_mirroring)
            {
                // Leave the scene objects as a host would find them: no mirrored invaders, a simulating formation. Skipped while the scene unloads.
                _mirroring = false;
                _appliedGeneration = -1;
                if (_formation != null && _ufo != null && gameObject.scene.isLoaded)
                {
                    _formation.Clear();
                    _formation.SimulationEnabled = true;
                    _ufo.Mirror(false, Vector2.zero);
                }
            }
        }

        /// <summary>Host only: clears the replicated world, for the end of a match.</summary>
        public void ServerReset()
        {
            if (!IsServer)
            {
                return;
            }

            _publishedWave = 0;
            _generation++;
            _formationState.Value = new FormationSnapshot { Generation = _generation };
            _ufoState.Value = default;
        }

        private void SpawnServerSide()
        {
            _healthBuffer = new byte[_bunkers.BlockCount];
            _bunkers.ReadHealths(_healthBuffer);
            _bunkerState.Clear();
            foreach (byte health in _healthBuffer)
            {
                _bunkerState.Add(health);
            }

            _bus.Subscribe<WaveStarted>(OnWaveStarted).AddTo(_subscriptions);
            _bus.Subscribe<InvaderMarched>(_ => PublishFormation()).AddTo(_subscriptions);
            _bus.Subscribe<InvaderDestroyed>(_ => PublishFormation()).AddTo(_subscriptions);
        }

        private void Update()
        {
            if (!IsServer || !IsSpawned)
            {
                return;
            }

            UfoSnapshot ufo = new() { Visible = _ufo.IsFlying, Position = _ufo.transform.position };
            if (!_ufoState.Value.Equals(ufo))
            {
                _ufoState.Value = ufo;
            }

            _bunkerTimer += Time.deltaTime;
            if (_bunkerTimer >= BunkerPollSeconds)
            {
                _bunkerTimer = 0f;
                PublishBunkers();
            }
        }

        private void OnWaveStarted(WaveStarted message)
        {
            _publishedWave = message.Number;
            _generation++;
            PublishFormation();
        }

        private void PublishFormation()
        {
            if (_publishedWave <= 0)
            {
                return;
            }

            _formation.WriteAliveMask(out ulong low, out ulong high);
            _formationState.Value = new FormationSnapshot
            {
                Generation = _generation,
                Wave = _publishedWave,
                DropRows = _waves.StartDropRows(_publishedWave),
                Origin = _formation.transform.position,
                StepIndex = _formation.StepIndex,
                AliveLow = low,
                AliveHigh = high
            };
        }

        private void PublishBunkers()
        {
            _bunkers.ReadHealths(_healthBuffer);
            for (int i = 0; i < _healthBuffer.Length && i < _bunkerState.Count; i++)
            {
                if (_bunkerState[i] != _healthBuffer[i])
                {
                    _bunkerState[i] = _healthBuffer[i];
                }
            }
        }

        private void OnFormationChanged(FormationSnapshot previous, FormationSnapshot current)
        {
            ApplyFormation(current);
        }

        private void OnUfoChanged(UfoSnapshot previous, UfoSnapshot current)
        {
            ApplyUfo(current);
        }

        private void OnBunkersChanged(NetworkListEvent<byte> change)
        {
            ApplyBunkers();
        }

        private void ApplyFormation(FormationSnapshot snapshot)
        {
            if (snapshot.IsEmpty)
            {
                _formation.Clear();
                _appliedGeneration = snapshot.Generation;
                return;
            }

            if (snapshot.Generation != _appliedGeneration)
            {
                _formation.Clear();
                _formation.Spawn(_waves.GetWave(snapshot.Wave), snapshot.DropRows);
                _appliedGeneration = snapshot.Generation;
            }

            _formation.ApplyMirror(snapshot.Origin, snapshot.StepIndex, snapshot.AliveLow, snapshot.AliveHigh);
        }

        private void ApplyUfo(UfoSnapshot snapshot)
        {
            _ufo.Mirror(snapshot.Visible, snapshot.Position);
        }

        private void ApplyBunkers()
        {
            for (int i = 0; i < _bunkerState.Count; i++)
            {
                _bunkers.SetHealth(i, _bunkerState[i]);
            }
        }
    }
}
