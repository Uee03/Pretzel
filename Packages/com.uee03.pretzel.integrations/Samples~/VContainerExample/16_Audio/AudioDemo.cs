using System;
using Pretzel.Audio;
using UnityEngine;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>
    /// Plays a generated tone through <see cref="IAudioService"/>. Gameplay code like this takes the interface in its
    /// constructor, so a test can inject a fake and the sample needs no audio assets. The timer comes from 05_Timers.
    /// </summary>
    public sealed class AudioDemo : IStartable, IDisposable
    {
        private const string Topic = "16 Audio";

        private readonly ISampleLog _log;
        private readonly IAudioService _audio;
        private readonly ITimerService _timers;
        private AudioClip _tone;

        public AudioDemo(ISampleLog log, IAudioService audio, ITimerService timers)
        {
            _log = log;
            _audio = audio;
            _timers = timers;
        }

        public void Start()
        {
            _tone = ToneClip.Create(660f, 0.4f);
            _audio.SetVolume(AudioChannel.UI, 0.5f);

            AudioHandle handle = _audio.PlayClip(_tone, AudioChannel.UI);
            _log.Write(Topic, $"Played a tone on the UI channel through {_audio.GetType().Name} (volume {_audio.GetVolume(AudioChannel.UI):0.0})");

            // A handle goes stale when its sound ends, so asking later is always safe.
            _timers.Start(1f, () => _log.Write(Topic, _audio.IsPlaying(handle) ? "The tone still plays" : "The tone ended; its handle is stale"));
        }

        public void Dispose()
        {
            if (_tone)
            {
                UnityEngine.Object.Destroy(_tone);
            }
        }
    }
}
