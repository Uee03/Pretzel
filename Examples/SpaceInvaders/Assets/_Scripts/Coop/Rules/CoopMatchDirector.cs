using System;
using System.Threading;
using Pretzel.Core;
using Pretzel.Events;
using Pretzel.Multiplayer;
using SpaceInvaders.Gameplay;
using UnityEngine;
using VContainer.Unity;

namespace SpaceInvaders.Coop
{
    /// <summary>
    /// The rules of a co-op match, in plain C# and for the host only: waves, each player's lives and score, and the end of the game.
    /// It mirrors the single-player level presenter, but the state is per player and every change is pushed to the replicated
    /// <see cref="CoopMatch"/>. On a client it does nothing: clients only show what the host reports.
    /// </summary>
    public sealed class CoopMatchDirector : IStartable, IDisposable
    {
        private const float FirstWaveDelaySeconds = 1.5f;
        private const float NextWaveDelaySeconds = 1.8f;

        private readonly CoopMatch _match;
        private readonly PretzelNetworkBootstrap _bootstrap;
        private readonly IEventBus _bus;
        private readonly CoopRunState _run;
        private readonly WaveSet _waves;
        private readonly InvaderFormation _formation;
        private readonly UfoSpawner _ufo;
        private readonly BunkerField _bunkers;
        private readonly CoopWorldReplicator _world;
        private readonly CompositeDisposable _subscriptions = new();
        private CancellationTokenSource _cancellation = new();
        private int _wave;

        public CoopMatchDirector(
            CoopMatch match,
            PretzelNetworkBootstrap bootstrap,
            IEventBus bus,
            CoopRunState run,
            WaveSet waves,
            InvaderFormation formation,
            UfoSpawner ufo,
            BunkerField bunkers,
            CoopWorldReplicator world)
        {
            _match = match;
            _bootstrap = bootstrap;
            _bus = bus;
            _run = run;
            _waves = waves;
            _formation = formation;
            _ufo = ufo;
            _bunkers = bunkers;
            _world = world;
        }

        private bool IsHost => _match != null && _match.IsSpawned && _match.IsServer && !_match.NetworkManager.ShutdownInProgress;

        public void Start()
        {
            if (_bootstrap.Service != null)
            {
                _bootstrap.Service.StateChanged += OnSessionStateChanged;
            }

            _match.ShipReady += OnShipReady;
            _match.ShipRemoved += OnShipRemoved;
            _match.ShipHit += OnShipHit;
            _bus.Subscribe<InvaderDestroyed>(OnInvaderDestroyed).AddTo(_subscriptions);
            _bus.Subscribe<UfoDestroyed>(OnUfoDestroyed).AddTo(_subscriptions);
            _bus.Subscribe<InvadersLanded>(_ => OnInvadersLanded()).AddTo(_subscriptions);
        }

        public void Dispose()
        {
            if (_bootstrap != null && _bootstrap.Service != null)
            {
                _bootstrap.Service.StateChanged -= OnSessionStateChanged;
            }

            _match.ShipReady -= OnShipReady;
            _match.ShipRemoved -= OnShipRemoved;
            _match.ShipHit -= OnShipHit;
            _subscriptions.Dispose();
            _cancellation.Cancel();
            _cancellation.Dispose();
        }

        private void OnSessionStateChanged(ConnectionState previous, ConnectionState current)
        {
            if (!IsHost)
            {
                return;
            }

            if (current == ConnectionState.Playing)
            {
                BeginMatch();
            }
            else if (previous == ConnectionState.Playing)
            {
                ReturnToLobby();
            }
        }

        private void BeginMatch()
        {
            RestartWork();
            _run.Reset();
            foreach (CoopShip ship in _match.Ships)
            {
                // A player in the reconnect grace window has a slot but no controller; they still start the new match with full lives.
                if (ship != null && ship.HasSlot)
                {
                    if (_run.AddPlayer(ship.Slot) != null)
                    {
                        ship.ServerRevive();
                    }
                    else
                    {
                        ship.ServerEliminate();
                    }
                }
            }

            _match.ServerSyncStats(_run);
            _match.ServerSetWave(0);
            _match.ServerSetPhase(CoopPhase.Playing);
            _ = StartFirstWaveAsync(_cancellation.Token);
        }

        private void ReturnToLobby()
        {
            RestartWork();
            _formation.Clear();
            _ufo.Stop();
            _bunkers.RestoreAll();
            _world.ServerReset();
            _run.Reset();
            _wave = 0;
            _match.ServerSyncStats(_run);
            _match.ServerSetWave(0);
            _match.ServerSetPhase(CoopPhase.Lobby);
        }

        private async Awaitable StartFirstWaveAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Awaitable.WaitForSecondsAsync(FirstWaveDelaySeconds, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (IsHost && _match.Phase == CoopPhase.Playing)
            {
                StartWaveSafely(1);
            }
        }

        // The waits are not awaited by anyone, so a failure here must be logged or the match would stall without a word.
        private void StartWaveSafely(int number)
        {
            try
            {
                BeginWave(number);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void BeginWave(int number)
        {
            _wave = number;
            WaveDefinition definition = _waves.GetWave(number);
            if (definition.Columns * definition.Rows.Count > FormationSnapshot.MaxInvaders)
            {
                throw new InvalidOperationException(
                    $"Wave {number} has {definition.Columns * definition.Rows.Count} invaders; co-op replicates at most {FormationSnapshot.MaxInvaders} (one bit each).");
            }

            _bunkers.RestoreAll();
            _formation.Spawn(definition, _waves.StartDropRows(number));
            _ufo.Begin();
            _match.ServerSetWave(number);
            _bus.Publish(new WaveStarted(number, definition.Title));
        }

        private async Awaitable AdvanceWaveAsync(int clearedWave, CancellationToken cancellationToken)
        {
            _ufo.Stop();
            _bus.Publish(new WaveCleared(clearedWave));
            try
            {
                await Awaitable.WaitForSecondsAsync(NextWaveDelaySeconds, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (IsHost && _match.Phase == CoopPhase.Playing && _wave == clearedWave)
            {
                StartWaveSafely(clearedWave + 1);
            }
        }

        private void OnShipReady(CoopShip ship)
        {
            if (!IsHost)
            {
                return;
            }

            if (_run.AddPlayer(ship.Slot) == null)
            {
                // A third ship (two on one machine plus a remote player) has no panel and no place in the rules: it sits out.
                ship.ServerEliminate();
                return;
            }

            // Between matches a ship is revived when the next one starts; only a player who joins a running match plays at once.
            if (_match.Phase == CoopPhase.Playing)
            {
                ship.ServerRevive();
            }

            _match.ServerSyncStats(_run);
        }

        private void OnShipRemoved(CoopShip ship)
        {
            // HasSlot, not IsSlotKnown: a player whose grace window ran out has no controller any more but still has an entry to remove.
            if (!IsHost || !ship.HasSlot)
            {
                return;
            }

            _run.RemovePlayer(ship.Slot);
            _match.ServerSyncStats(_run);
            CheckGameOver();
        }

        private void OnShipHit(CoopShip ship)
        {
            int livesLeft = _run.LoseLife(ship.Slot);
            if (livesLeft == 0)
            {
                ship.ServerEliminate();
            }

            _match.ServerSyncStats(_run);
            CheckGameOver();
        }

        private void OnInvaderDestroyed(InvaderDestroyed message)
        {
            if (!IsHost)
            {
                return;
            }

            Credit(message.Points);
            if (_formation.AliveCount == 0 && _match.Phase == CoopPhase.Playing)
            {
                _ = AdvanceWaveAsync(_wave, _cancellation.Token);
            }
        }

        private void OnUfoDestroyed(UfoDestroyed message)
        {
            if (IsHost)
            {
                Credit(message.Points);
            }
        }

        private void OnInvadersLanded()
        {
            if (!IsHost || _match.Phase != CoopPhase.Playing)
            {
                return;
            }

            _run.EliminateAll();
            foreach (CoopShip ship in _match.Ships)
            {
                if (ship != null)
                {
                    ship.ServerEliminate();
                }
            }

            _match.ServerSyncStats(_run);
            EndGame();
        }

        private void Credit(int points)
        {
            int slot = _match.Projectiles.CurrentShooterSlot;
            if (slot < 0)
            {
                return;
            }

            _run.AddScore(slot, points);
            _match.ServerSyncStats(_run);
        }

        private void CheckGameOver()
        {
            if (_run.AllEliminated)
            {
                EndGame();
            }
        }

        private void EndGame()
        {
            if (_match.Phase != CoopPhase.Playing)
            {
                return;
            }

            _formation.Halt();
            _ufo.Stop();
            _cancellation.Cancel();
            _bus.Publish(new GameEnded(_run.TotalScore, 0, false));
            _match.ServerSetPhase(CoopPhase.GameOver);
        }

        private void RestartWork()
        {
            _cancellation.Cancel();
            _cancellation.Dispose();
            _cancellation = new CancellationTokenSource();
        }
    }
}
