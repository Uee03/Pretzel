using System;
using Pretzel.Audio;
using Pretzel.Core;
using Pretzel.Events;
using Pretzel.Sfx;
using SpaceInvaders.Gameplay;
using UnityEngine;
using VContainer.Unity;

namespace SpaceInvaders.Audio
{
    /// <summary>Plays the right sound for each game event, by id, through the SFX player. The game logic never knows audio exists.</summary>
    public sealed class GameAudioReactions : IStartable, IDisposable
    {
        private const float MarchVolume = 0.45f;

        private readonly IEventBus _bus;
        private readonly ISfxPlayer _sfx;
        private readonly IAudioService _audio;
        private readonly SoundBank _sounds;
        private readonly CompositeDisposable _subscriptions = new();

        public GameAudioReactions(IEventBus bus, ISfxPlayer sfx, IAudioService audio, SoundBank sounds)
        {
            _bus = bus;
            _sfx = sfx;
            _audio = audio;
            _sounds = sounds;
        }

        public void Start()
        {
            _bus.Subscribe<PlayerFired>(_ => _sfx.Play(GameSounds.PlayerShot)).AddTo(_subscriptions);
            _bus.Subscribe<InvaderDestroyed>(_ => _sfx.Play(GameSounds.InvaderDestroyed)).AddTo(_subscriptions);
            _bus.Subscribe<UfoDestroyed>(_ => _sfx.Play(GameSounds.UfoDestroyed)).AddTo(_subscriptions);
            _bus.Subscribe<PlayerHit>(_ => _sfx.Play(GameSounds.PlayerHit)).AddTo(_subscriptions);
            _bus.Subscribe<WaveCleared>(_ => _sfx.Play(GameSounds.WaveCleared)).AddTo(_subscriptions);
            _bus.Subscribe<GameEnded>(_ => _sfx.Play(GameSounds.GameOver)).AddTo(_subscriptions);
            _bus.Subscribe<InvaderMarched>(OnMarched).AddTo(_subscriptions);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }

        private void OnMarched(InvaderMarched message)
        {
            AudioClip note = _sounds.MarchNote(message.StepIndex);
            if (note)
            {
                _audio.PlayClip(note, AudioChannel.Sfx, MarchVolume);
            }
        }
    }
}
