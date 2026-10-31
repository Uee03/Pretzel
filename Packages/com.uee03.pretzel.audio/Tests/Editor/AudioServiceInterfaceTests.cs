using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Pretzel.Audio.Tests
{
    public sealed class AudioServiceInterfaceTests
    {
        [Test]
        public void AudioServiceImplementsTheInterface()
        {
            Assert.That(typeof(IAudioService).IsAssignableFrom(typeof(AudioService)), Is.True);
        }

        [Test]
        public void GameplayCodeRunsAgainstAFakeService()
        {
            FakeAudioService fake = new();
            DoorSounds door = new(fake, null);

            door.Open();
            door.Close();

            Assert.That(fake.Calls, Is.EqualTo(new[] { "Play(null)", "StopAll" }));
        }

        [Test]
        public void VolumeAndMusicGoThroughTheInterface()
        {
            IAudioService audio = new FakeAudioService();
            audio.SetVolume(AudioChannel.Music, 0.25f);
            Assert.That(audio.GetVolume(AudioChannel.Music), Is.EqualTo(0.25f));
            Assert.That(audio.GetVolume(AudioChannel.Sfx), Is.EqualTo(1f));
        }

        // The consumer takes the interface, so the test needs no AudioSource, mixer or scene.
        private sealed class DoorSounds
        {
            private readonly IAudioService _audio;
            private readonly AudioCue _openCue;

            public DoorSounds(IAudioService audio, AudioCue openCue)
            {
                _audio = audio;
                _openCue = openCue;
            }

            public void Open() => _audio.Play(_openCue);

            public void Close() => _audio.StopAll();
        }

        private sealed class FakeAudioService : IAudioService
        {
            private readonly List<string> _calls = new();
            private readonly Dictionary<AudioChannel, float> _volumes = new();

            public IReadOnlyList<string> Calls => _calls;

            public AudioClip CurrentMusic => null;

            public AudioHandle Play(AudioCue cue)
            {
                _calls.Add("Play(" + (cue ? cue.name : "null") + ")");
                return AudioHandle.None;
            }

            public AudioHandle Play(AudioCue cue, Vector3 position) => Play(cue);

            public AudioHandle PlayClip(AudioClip clip, AudioChannel channel = AudioChannel.Sfx, float volume = 1f) => AudioHandle.None;

            public bool IsPlaying(AudioHandle handle) => false;

            public void Stop(AudioHandle handle)
            {
            }

            public void StopAll() => _calls.Add("StopAll");

            public void PlayMusic(AudioClip clip, float fadeSeconds = -1f, float volume = 1f)
            {
            }

            public void StopMusic(float fadeSeconds = -1f)
            {
            }

            public void SetVolume(AudioChannel channel, float linear) => _volumes[channel] = linear;

            public float GetVolume(AudioChannel channel) => _volumes.TryGetValue(channel, out float value) ? value : 1f;
        }
    }
}
