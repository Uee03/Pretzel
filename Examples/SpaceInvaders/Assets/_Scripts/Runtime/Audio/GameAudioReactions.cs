using System;
using Pretzel.Audio;
using Pretzel.Core;
using Pretzel.Events;
using SpaceInvaders.Gameplay;
using UnityEngine;
using VContainer.Unity;

namespace SpaceInvaders.Audio
{
    /// <summary>Plays the right sound for each game event. The game logic never knows audio exists.</summary>
    public sealed class GameAudioReactions : IStartable, IDisposable
    {
        private readonly IEventBus _bus;
        private readonly IAudioService _audio;
        private readonly SoundBank _sounds;
        private readonly CompositeDisposable _subscriptions = new();

        public GameAudioReactions(IEventBus bus, IAudioService audio, SoundBank sounds)
        {
            _bus = bus;
            _audio = audio;
            _sounds = sounds;
        }

        public void Start()
        {
            _bus.Subscribe<PlayerFired>(_ => Play(_sounds.PlayerShot)).AddTo(_subscriptions);
            _bus.Subscribe<InvaderDestroyed>(_ => Play(_sounds.InvaderDestroyed)).AddTo(_subscriptions);
            _bus.Subscribe<UfoDestroyed>(_ => Play(_sounds.UfoDestroyed)).AddTo(_subscriptions);
            _bus.Subscribe<PlayerHit>(_ => Play(_sounds.PlayerHit)).AddTo(_subscriptions);
            _bus.Subscribe<WaveCleared>(_ => Play(_sounds.WaveCleared)).AddTo(_subscriptions);
            _bus.Subscribe<GameEnded>(_ => Play(_sounds.GameOver)).AddTo(_subscriptions);
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
                _audio.PlayClip(note, AudioChannel.Sfx, 0.6f);
            }
        }

        private void Play(AudioCue cue)
        {
            if (cue)
            {
                _audio.Play(cue);
            }
        }
    }
}
