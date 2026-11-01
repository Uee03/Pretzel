using System;
using Pretzel.Core;
using Pretzel.Events;
using Pretzel.UI;
using SpaceInvaders.Audio;
using SpaceInvaders.Gameplay;
using VContainer.Unity;

namespace SpaceInvaders.UI
{
    /// <summary>Keeps the HUD in step with the game: score, lives, wave, toasts, the game-over screen and the volume slider.</summary>
    public sealed class HudPresenter : IStartable, IDisposable
    {
        private readonly HudView _hud;
        private readonly IGameService _game;
        private readonly RunState _run;
        private readonly IEventBus _bus;
        private readonly MasterVolumeSetting _volume;
        private readonly CompositeDisposable _subscriptions = new();

        public HudPresenter(HudView hud, IGameService game, RunState run, IEventBus bus, MasterVolumeSetting volume)
        {
            _hud = hud;
            _game = game;
            _run = run;
            _bus = bus;
            _volume = volume;
        }

        public void Start()
        {
            _game.ScoreChanged += OnScoreChanged;
            _run.LivesChanged += _hud.SetLives;
            _run.WaveChanged += _hud.SetWave;
            _hud.VolumeChanged += _volume.Set;
            _hud.Bound += Refresh;
            _bus.Subscribe<WaveStarted>(message => _hud.ShowToast($"WAVE {message.Number}", ToastKind.Success)).AddTo(_subscriptions);
            _bus.Subscribe<UfoDestroyed>(message => _hud.ShowToast($"MYSTERY SHIP  +{message.Points}", ToastKind.Warning)).AddTo(_subscriptions);
            _bus.Subscribe<GameEnded>(message => _hud.ShowGameOver(message.Score, message.HighScore, message.IsNewHighScore)).AddTo(_subscriptions);
            Refresh();
        }

        public void Dispose()
        {
            _game.ScoreChanged -= OnScoreChanged;
            _run.LivesChanged -= _hud.SetLives;
            _run.WaveChanged -= _hud.SetWave;
            _hud.VolumeChanged -= _volume.Set;
            _hud.Bound -= Refresh;
            _subscriptions.Dispose();
        }

        private void Refresh()
        {
            _hud.SetScore(_game.Score);
            _hud.SetHighScore(Math.Max(_game.HighScore, _game.Score));
            _hud.SetLives(_run.Lives);
            _hud.SetWave(_run.Wave);
            _hud.SetVolume(_volume.Value);
        }

        private void OnScoreChanged(int score)
        {
            _hud.SetScore(score);
            _hud.SetHighScore(Math.Max(_game.HighScore, score));
        }
    }
}
