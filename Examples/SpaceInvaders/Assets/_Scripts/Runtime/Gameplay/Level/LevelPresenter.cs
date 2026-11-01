using System;
using System.Threading;
using Pretzel.Core;
using Pretzel.Events;
using SpaceInvaders.UI;
using UnityEngine;
using VContainer.Unity;

namespace SpaceInvaders.Gameplay
{
    /// <summary>
    /// The rules of one game, in plain C#: waves, lives, score, pause and game over. It reacts to events from the actors and tells them
    /// what to do next; it never reaches into a prefab or a UI element itself.
    /// </summary>
    public sealed class LevelPresenter : IStartable, ITickable, IDisposable
    {
        private const float NextWaveDelaySeconds = 1.8f;

        private readonly IEventBus _bus;
        private readonly IGameService _game;
        private readonly IGameNavigator _navigator;
        private readonly RunState _run;
        private readonly WaveSet _waves;
        private readonly InvaderFormation _formation;
        private readonly PlayerShip _player;
        private readonly UfoSpawner _ufo;
        private readonly BunkerField _bunkers;
        private readonly HudView _hud;
        private readonly ShipControls _controls;
        private readonly GameInput _input;
        private readonly CompositeDisposable _subscriptions = new();
        private readonly CancellationTokenSource _cancellation = new();

        public LevelPresenter(
            IEventBus bus,
            IGameService game,
            IGameNavigator navigator,
            RunState run,
            WaveSet waves,
            InvaderFormation formation,
            PlayerShip player,
            UfoSpawner ufo,
            BunkerField bunkers,
            HudView hud,
            ShipControls controls,
            GameInput input)
        {
            _bus = bus;
            _game = game;
            _navigator = navigator;
            _run = run;
            _waves = waves;
            _formation = formation;
            _player = player;
            _ufo = ufo;
            _bunkers = bunkers;
            _hud = hud;
            _controls = controls;
            _input = input;
        }

        public void Start()
        {
            _run.Reset();
            _game.StartGame();

            // A game left from the pause screen must not leave the Player map off for the next one.
            _input.Resume();
            _bus.Subscribe<InvaderDestroyed>(OnInvaderDestroyed).AddTo(_subscriptions);
            _bus.Subscribe<UfoDestroyed>(message => _game.AddScore(message.Points)).AddTo(_subscriptions);
            _bus.Subscribe<PlayerHit>(_ => OnPlayerHit()).AddTo(_subscriptions);
            _bus.Subscribe<InvadersLanded>(_ => OnInvadersLanded()).AddTo(_subscriptions);
            _hud.ResumeRequested += Resume;
            _hud.RetryRequested += Retry;
            _hud.QuitRequested += Quit;
            BeginWave(_run.NextWave());
        }

        public void Tick()
        {
            if (!_controls.PausePressed)
            {
                return;
            }

            if (_game.State == GameState.Playing)
            {
                _game.Pause();
                _input.Pause();
                _hud.ShowPause();
            }
            else if (_game.State == GameState.Paused)
            {
                Resume();
            }
        }

        public void Dispose()
        {
            _input.Resume();
            _cancellation.Cancel();
            _cancellation.Dispose();
            _subscriptions.Dispose();
            _hud.ResumeRequested -= Resume;
            _hud.RetryRequested -= Retry;
            _hud.QuitRequested -= Quit;
        }

        /// <summary>Skips to a wave. Used by the dev console.</summary>
        public void JumpToWave(int wave)
        {
            if (_game.State != GameState.Playing)
            {
                return;
            }

            _run.SetWave(wave);
            BeginWave(_run.Wave);
        }

        private void BeginWave(int number)
        {
            WaveDefinition definition = _waves.GetWave(number);
            _bunkers.RestoreAll();
            _formation.Spawn(definition, _waves.StartDropRows(number));
            _ufo.Begin();
            _bus.Publish(new WaveStarted(number, definition.Title));
        }

        private void OnInvaderDestroyed(InvaderDestroyed message)
        {
            _game.AddScore(message.Points);
            if (_formation.AliveCount == 0 && _game.State == GameState.Playing)
            {
                _ = AdvanceWaveAsync(_run.Wave);
            }
        }

        private async Awaitable AdvanceWaveAsync(int clearedWave)
        {
            _ufo.Stop();
            _bus.Publish(new WaveCleared(clearedWave));
            try
            {
                await Awaitable.WaitForSecondsAsync(NextWaveDelaySeconds, _cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (_game.State == GameState.Playing && _run.Wave == clearedWave)
            {
                BeginWave(_run.NextWave());
            }
        }

        private void OnPlayerHit()
        {
            if (_run.LoseLife() <= 0)
            {
                EndGame();
            }
        }

        private void OnInvadersLanded()
        {
            _run.LoseAllLives();
            EndGame();
        }

        private void EndGame()
        {
            if (_game.State != GameState.Playing)
            {
                return;
            }

            _formation.Halt();
            _ufo.Stop();
            _player.Eliminate();
            int score = _game.Score;
            bool isNewHighScore = score > _game.HighScore;
            _game.EndGame();
            _bus.Publish(new GameEnded(score, _game.HighScore, isNewHighScore));
        }

        private void Resume()
        {
            _game.Resume();
            _input.Resume();
            _hud.HidePause();
        }

        private void Retry()
        {
            _ = _navigator.PlayAsync();
        }

        private void Quit()
        {
            _ = _navigator.ReturnToTitleAsync();
        }
    }
}
