using System;
using System.Collections.Generic;
using System.Threading;
using Pretzel.Core;
using Pretzel.Core.Timers;
using Pretzel.Events;
using Pretzel.Extensions;
using Pretzel.Pooling;
using UnityEngine;
using VContainer;

namespace SpaceInvaders.Gameplay
{
    /// <summary>
    /// The marching block of invaders. Owns the keyed invader pools, steps the formation on a countdown that speeds up as invaders
    /// die, and drops bombs from the lowest invader of a random column.
    /// </summary>
    public sealed class InvaderFormation : MonoBehaviour
    {
        [SerializeField] private PoolFactory<InvaderKind, Invader> _invaders = new();
        [SerializeField] private float _leftLimit = -7.6f;
        [SerializeField] private float _rightLimit = 7.6f;
        [SerializeField] private float _halfWidth = 0.45f;
        [SerializeField] private float _stepX = 0.3f;
        [SerializeField] private float _dropY = 0.4f;
        [SerializeField] private float _startY = 2.9f;
        [SerializeField] private float _landingY = -2.3f;

        private readonly List<Invader> _alive = new();
        private readonly List<Invader> _shooters = new();
        private readonly CountdownTimer _march = new(1f);
        private readonly Cooldown _bombs = new(1f);
        private readonly MarchLogic _logic = new();
        private IEventBus _bus;
        private IProjectileSpawner _projectiles;
        private WaveDefinition _wave;
        private Invader[] _lowestPerColumn = new Invader[0];
        private int _total;
        private int _stepIndex;
        private bool _running;

        public int AliveCount => _alive.Count;

        public IReadOnlyList<Invader> Alive => _alive;

        public bool IsRunning => _running;

        /// <summary>Off on a client that only mirrors the host's formation: <see cref="Spawn"/> then builds the invaders but never marches or bombs.</summary>
        public bool SimulationEnabled { get; set; } = true;

        /// <summary>The wave being played, or null before the first <see cref="Spawn"/>.</summary>
        public WaveDefinition Wave => _wave;

        /// <summary>Marches taken in this wave. The invaders' animation frame follows it.</summary>
        public int StepIndex => _stepIndex;

        // Runs when the level scope builds, before Awake: keep it to storing dependencies.
        [Inject]
        public void Construct(IEventBus bus, IProjectileSpawner projectiles)
        {
            _bus = Guard.NotNull(bus, nameof(bus));
            _projectiles = Guard.NotNull(projectiles, nameof(projectiles));
        }

        private void Awake()
        {
            _invaders.Initialize();
            _march.Completed += OnMarchStep;
        }

        private void OnDestroy()
        {
            _march.Completed -= OnMarchStep;
            _invaders.Dispose();
        }

        private void Update()
        {
            if (!_running)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            _march.Tick(deltaTime);
            _bombs.Tick(deltaTime);
            TryDropBomb();
        }

        /// <summary>Creates the invaders of <paramref name="wave"/> and starts marching.</summary>
        public void Spawn(WaveDefinition wave, int dropRows)
        {
            Guard.NotNull(wave, nameof(wave));
            ReleaseAll();
            _wave = wave;
            _logic.Reset();
            _stepIndex = 0;
            transform.position = new Vector3(0f, _startY - dropRows * _dropY, 0f);

            IReadOnlyList<InvaderKind> rows = wave.Rows;
            float centre = (wave.Columns - 1) * 0.5f;
            for (int row = 0; row < rows.Count; row++)
            {
                for (int column = 0; column < wave.Columns; column++)
                {
                    Invader invader = _invaders.Spawn(rows[row], transform.position, Quaternion.identity, transform);
                    invader.transform.localPosition = new Vector3((column - centre) * wave.Spacing.x, -row * wave.Spacing.y, 0f);
                    invader.Column = column;
                    invader.Row = row;
                    invader.Killed += OnInvaderKilled;
                    _alive.Add(invader);
                }
            }

            _total = _alive.Count;
            if (_lowestPerColumn.Length != wave.Columns)
            {
                _lowestPerColumn = new Invader[wave.Columns];
            }

            _march.Reset(MarchTempo.StepInterval(_alive.Count, _total, wave.SlowestStepSeconds, wave.FastestStepSeconds));
            _march.Start();
            _bombs.Duration = wave.BombIntervalSeconds;
            _bombs.Reset();
            _bombs.TryUse();
            _running = SimulationEnabled;
        }

        /// <summary>Stops marching and bombing, for a pause between waves or the end of the game.</summary>
        public void Halt()
        {
            _running = false;
        }

        /// <summary>Kills every invader as if shot by the player. Used by the dev console.</summary>
        public void KillAll()
        {
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                if (i < _alive.Count)
                {
                    _alive[i].TakeHit(Team.Player);
                }
            }
        }

        /// <summary>Creates the pooled invaders ahead of the wave, a few per frame, so spawning the wave never hitches.</summary>
        public async Awaitable PrewarmAsync(int columns, IReadOnlyList<InvaderKind> rows, IProgress<float> progress, CancellationToken cancellationToken)
        {
            Dictionary<InvaderKind, int> needed = new();
            foreach (InvaderKind kind in rows)
            {
                needed[kind] = needed.TryGetValue(kind, out int count) ? count + columns : columns;
            }

            int total = rows.Count * columns;
            int done = 0;
            FrameBudget budget = new(4f);
            foreach (KeyValuePair<InvaderKind, int> pair in needed)
            {
                for (int i = 1; i <= pair.Value; i++)
                {
                    _invaders.Prewarm(pair.Key, i);
                    done++;
                    progress?.Report(done / (float)total);
                    await budget.YieldIfExceededAsync(cancellationToken);
                }
            }
        }

        private void OnMarchStep()
        {
            if (_alive.Count == 0)
            {
                return;
            }

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float lowestY = float.MaxValue;
            foreach (Invader invader in _alive)
            {
                Vector3 position = invader.transform.position;
                minX = Mathf.Min(minX, position.x - _halfWidth);
                maxX = Mathf.Max(maxX, position.x + _halfWidth);
                lowestY = Mathf.Min(lowestY, position.y);
            }

            Vector2 step = _logic.NextStep(minX, maxX, _leftLimit, _rightLimit, _stepX, _dropY);
            transform.position += (Vector3)step;
            foreach (Invader invader in _alive)
            {
                invader.AdvanceFrame();
            }

            _bus.Publish(new InvaderMarched(_stepIndex++));
            if (lowestY + step.y <= _landingY)
            {
                _running = false;
                _bus.Publish(new InvadersLanded());
                return;
            }

            _march.Reset(MarchTempo.StepInterval(_alive.Count, _total, _wave.SlowestStepSeconds, _wave.FastestStepSeconds));
            _march.Start();
        }

        private void TryDropBomb()
        {
            if (_alive.Count == 0 || !_bombs.IsReady || _projectiles.CountActive(ProjectileKind.Bomb) >= _wave.MaxBombs)
            {
                return;
            }

            Array.Clear(_lowestPerColumn, 0, _lowestPerColumn.Length);
            foreach (Invader invader in _alive)
            {
                Invader current = _lowestPerColumn[invader.Column];
                if (current == null || invader.transform.position.y < current.transform.position.y)
                {
                    _lowestPerColumn[invader.Column] = invader;
                }
            }

            _shooters.Clear();
            foreach (Invader candidate in _lowestPerColumn)
            {
                if (candidate != null)
                {
                    _shooters.Add(candidate);
                }
            }

            Invader shooter = _shooters.GetRandomItem();
            _projectiles.Spawn(ProjectileKind.Bomb, shooter.transform.position + Vector3.down * 0.4f);
            _bombs.Duration = _wave.BombIntervalSeconds * UnityEngine.Random.Range(0.6f, 1.4f);
            _bombs.TryUse();
        }

        private void OnInvaderKilled(Invader invader)
        {
            _alive.Remove(invader);
            _bus.Publish(new InvaderDestroyed(invader.Kind, invader.transform.position, ScoreTable.PointsFor(invader.Kind)));
            _invaders.Release(invader);
            if (_alive.Count == 0)
            {
                _running = false;
            }
        }

        /// <summary>Removes every invader without publishing events. Used when a match ends.</summary>
        public void Clear()
        {
            ReleaseAll();
            _running = false;
        }

        /// <summary>Which invaders are alive, one bit per slot of the grid (row * columns + column), 128 slots at most.</summary>
        public void WriteAliveMask(out ulong low, out ulong high)
        {
            low = 0UL;
            high = 0UL;
            if (_wave == null)
            {
                return;
            }

            foreach (Invader invader in _alive)
            {
                int slot = invader.Row * _wave.Columns + invader.Column;
                if (slot < 64)
                {
                    low |= 1UL << slot;
                }
                else if (slot < 128)
                {
                    high |= 1UL << (slot - 64);
                }
            }
        }

        /// <summary>
        /// Moves a mirrored formation to the host's state: the block position, the animation frame and the surviving invaders.
        /// Invaders missing from the mask leave silently, because the host's events already reach this machine another way.
        /// </summary>
        public void ApplyMirror(Vector2 origin, int stepIndex, ulong low, ulong high)
        {
            if (_wave == null)
            {
                return;
            }

            transform.position = new Vector3(origin.x, origin.y, 0f);
            _stepIndex = stepIndex;
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                Invader invader = _alive[i];
                int slot = invader.Row * _wave.Columns + invader.Column;
                bool present = slot < 64 ? (low & (1UL << slot)) != 0UL : slot < 128 && (high & (1UL << (slot - 64))) != 0UL;
                if (!present)
                {
                    invader.Killed -= OnInvaderKilled;
                    _alive.RemoveAt(i);
                    _invaders.Release(invader);
                    continue;
                }

                invader.ShowFrame(stepIndex);
            }
        }

        private void ReleaseAll()
        {
            foreach (Invader invader in _alive)
            {
                if (invader)
                {
                    invader.Killed -= OnInvaderKilled;
                    _invaders.Release(invader);
                }
            }

            _alive.Clear();
        }
    }
}
