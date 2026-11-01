using System;
using System.Collections.Generic;
using Pretzel.Core;
using Pretzel.Events;
using SpaceInvaders.Gameplay;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace SpaceInvaders.Coop
{
    /// <summary>
    /// The shared state of a co-op match and the hub the network objects find it through. The host writes the phase, the wave and the
    /// stats of every player; everyone reads them. The host also forwards the game events that clients need for sound and effects, and
    /// each client publishes them on its own event bus, so the audio, explosion and feel reactions work unchanged on every machine.
    /// </summary>
    public sealed class CoopMatch : NetworkBehaviour
    {
        private readonly NetworkVariable<CoopPhase> _phase = new(CoopPhase.Lobby, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _wave = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkList<CoopPlayerStat> _stats = new(null, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly List<CoopShip> _ships = new();
        private CompositeDisposable _relays = new();
        private IEventBus _bus;
        private WaveSet _waves;
        private CoopProjectileSpawner _projectiles;
        private int _localShips;

        public IEventBus Bus => _bus;

        public CoopProjectileSpawner Projectiles => _projectiles;

        public CoopPhase Phase => _phase.Value;

        public int Wave => _wave.Value;

        public int StatCount => _stats.Count;

        /// <summary>Every ship spawned on this machine, local or not.</summary>
        public IReadOnlyList<CoopShip> Ships => _ships;

        /// <summary>How many ships this machine controls. With one, both keyboard schemes steer it.</summary>
        public int LocalShipCount => _localShips;

        public event Action<CoopPhase> PhaseChanged;

        public event Action<int> WaveChanged;

        public event Action StatsChanged;

        public event Action<CoopShip> ShipAdded;

        public event Action<CoopShip> ShipRemoved;

        /// <summary>The number of ships this machine controls changed. The HUD uses it to say who "you" are.</summary>
        public event Action LocalShipsChanged;

        /// <summary>Host only: a ship got its slot and can take part in the match.</summary>
        public event Action<CoopShip> ShipReady;

        /// <summary>Host only: a bomb hit a ship that could be hit.</summary>
        public event Action<CoopShip> ShipHit;

        // Runs when the level scope builds, before Awake: keep it to storing dependencies.
        [Inject]
        public void Construct(IEventBus bus, WaveSet waves, CoopProjectileSpawner projectiles)
        {
            _bus = Guard.NotNull(bus, nameof(bus));
            _waves = Guard.NotNull(waves, nameof(waves));
            _projectiles = Guard.NotNull(projectiles, nameof(projectiles));
        }

        public override void OnNetworkSpawn()
        {
            _phase.OnValueChanged += OnPhaseChanged;
            _wave.OnValueChanged += OnWaveChanged;
            _stats.OnListChanged += OnStatsListChanged;
            if (IsServer)
            {
                ForwardEvents();
            }

            // A client that joins mid-match reads the current values once.
            PhaseChanged?.Invoke(_phase.Value);
            WaveChanged?.Invoke(_wave.Value);
            StatsChanged?.Invoke();
            if (!IsServer && _phase.Value == CoopPhase.Playing && _wave.Value > 0)
            {
                // A player who joins mid-wave missed the WaveStarted event; replay it locally so the music and the star field match the wave.
                _bus.Publish(new WaveStarted(_wave.Value, _waves.GetWave(_wave.Value).Title));
            }
        }

        public override void OnNetworkDespawn()
        {
            _phase.OnValueChanged -= OnPhaseChanged;
            _wave.OnValueChanged -= OnWaveChanged;
            _stats.OnListChanged -= OnStatsListChanged;
            _relays.Dispose();
            _relays = new CompositeDisposable();
        }

        public void RegisterShip(CoopShip ship)
        {
            if (_ships.Contains(ship))
            {
                return;
            }

            _ships.Add(ship);
            ShipAdded?.Invoke(ship);
        }

        public void UnregisterShip(CoopShip ship)
        {
            if (_ships.Remove(ship))
            {
                ShipRemoved?.Invoke(ship);
            }
        }

        public void SetLocalShipCount(int count)
        {
            int clamped = Mathf.Max(0, count);
            if (clamped == _localShips)
            {
                return;
            }

            _localShips = clamped;
            LocalShipsChanged?.Invoke();
        }

        public CoopShip FindShip(int slot)
        {
            foreach (CoopShip ship in _ships)
            {
                if (ship != null && ship.HasSlot && ship.Slot == slot)
                {
                    return ship;
                }
            }

            return null;
        }

        public void CopyStats(List<CoopPlayerStat> destination)
        {
            destination.Clear();
            for (int i = 0; i < _stats.Count; i++)
            {
                destination.Add(_stats[i]);
            }
        }

        public void ServerAnnounceShip(CoopShip ship)
        {
            if (IsServer)
            {
                ShipReady?.Invoke(ship);
            }
        }

        public void ServerReportHit(CoopShip ship)
        {
            if (IsServer)
            {
                ShipHit?.Invoke(ship);
            }
        }

        public void ServerSetPhase(CoopPhase phase)
        {
            if (IsServer && _phase.Value != phase)
            {
                _phase.Value = phase;
            }
        }

        public void ServerSetWave(int wave)
        {
            if (IsServer && _wave.Value != wave)
            {
                _wave.Value = wave;
            }
        }

        /// <summary>Copies the host's rules into the replicated list, touching only the entries that changed.</summary>
        public void ServerSyncStats(CoopRunState run)
        {
            if (!IsServer)
            {
                return;
            }

            IReadOnlyList<CoopPlayerRun> players = run.Players;
            for (int i = 0; i < players.Count; i++)
            {
                CoopPlayerStat stat = new() { Slot = players[i].Slot, Lives = players[i].Lives, Score = players[i].Score };
                if (i >= _stats.Count)
                {
                    _stats.Add(stat);
                }
                else if (!_stats[i].Equals(stat))
                {
                    _stats[i] = stat;
                }
            }

            while (_stats.Count > players.Count)
            {
                _stats.RemoveAt(_stats.Count - 1);
            }
        }

        private void OnPhaseChanged(CoopPhase previous, CoopPhase current)
        {
            PhaseChanged?.Invoke(current);
        }

        private void OnWaveChanged(int previous, int current)
        {
            WaveChanged?.Invoke(current);
        }

        private void OnStatsListChanged(NetworkListEvent<CoopPlayerStat> change)
        {
            StatsChanged?.Invoke();
        }

        private void ForwardEvents()
        {
            _bus.Subscribe<PlayerFired>(_ => PlayerFiredRpc()).AddTo(_relays);
            _bus.Subscribe<InvaderDestroyed>(message => InvaderDestroyedRpc((byte)message.Kind, message.Position, message.Points)).AddTo(_relays);
            _bus.Subscribe<InvaderMarched>(message => InvaderMarchedRpc(message.StepIndex)).AddTo(_relays);
            _bus.Subscribe<PlayerHit>(message => PlayerHitRpc(message.Position)).AddTo(_relays);
            _bus.Subscribe<UfoDestroyed>(message => UfoDestroyedRpc(message.Position, message.Points)).AddTo(_relays);
            _bus.Subscribe<WaveStarted>(message => WaveStartedRpc(message.Number)).AddTo(_relays);
            _bus.Subscribe<WaveCleared>(message => WaveClearedRpc(message.Number)).AddTo(_relays);
            _bus.Subscribe<GameEnded>(message => GameEndedRpc(message.Score)).AddTo(_relays);
        }

        // The host already ran these locally. SendTo.NotServer reaches every client but not the host's own process. Only the host may invoke them:
        // NGO's default permission is Everyone, which would let a modified client forge events and have the host relay them to the others.
        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        private void PlayerFiredRpc()
        {
            _bus.Publish(new PlayerFired());
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        private void InvaderDestroyedRpc(byte kind, Vector3 position, int points)
        {
            _bus.Publish(new InvaderDestroyed((InvaderKind)kind, position, points));
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        private void InvaderMarchedRpc(int stepIndex)
        {
            _bus.Publish(new InvaderMarched(stepIndex));
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        private void PlayerHitRpc(Vector3 position)
        {
            _bus.Publish(new PlayerHit(position));
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        private void UfoDestroyedRpc(Vector3 position, int points)
        {
            _bus.Publish(new UfoDestroyed(position, points));
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        private void WaveStartedRpc(int number)
        {
            _bus.Publish(new WaveStarted(number, _waves.GetWave(number).Title));
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        private void WaveClearedRpc(int number)
        {
            _bus.Publish(new WaveCleared(number));
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        private void GameEndedRpc(int totalScore)
        {
            _bus.Publish(new GameEnded(totalScore, 0, false));
        }
    }
}
