using System;
using Pretzel.Audio;
using Pretzel.Core;
using Pretzel.Events;
using SpaceInvaders.Gameplay;
using UnityEngine;
using VContainer.Unity;

namespace SpaceInvaders.Audio
{
    /// <summary>
    /// The music of a level: the gameplay loop at first, the last stand from wave 5 on, and a quiet fade-out when the game is
    /// lost. The game logic never knows music exists; the audio service crossfades and loops.
    /// </summary>
    public sealed class GameMusic : IStartable, IDisposable
    {
        public const float Volume = 0.55f;
        public const float WaveFadeSeconds = 1.5f;
        public const float GameOverFadeSeconds = 0.6f;

        private readonly IEventBus _bus;
        private readonly IAudioService _audio;
        private readonly SoundBank _sounds;
        private readonly CompositeDisposable _subscriptions = new();

        public GameMusic(IEventBus bus, IAudioService audio, SoundBank sounds)
        {
            _bus = bus;
            _audio = audio;
            _sounds = sounds;
        }

        // The first WaveStarted may be published before this class starts, so the first wave's tune is started here.
        public void Start()
        {
            PlayForWave(1);
            _bus.Subscribe<WaveStarted>(message => PlayForWave(message.Number)).AddTo(_subscriptions);
            _bus.Subscribe<GameEnded>(_ => _audio.StopMusic(GameOverFadeSeconds)).AddTo(_subscriptions);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }

        private void PlayForWave(int wave)
        {
            AudioClip clip = _sounds.MusicForWave(wave);
            if (clip != null)
            {
                _audio.PlayMusic(clip, WaveFadeSeconds, Volume);
            }
        }
    }
}
