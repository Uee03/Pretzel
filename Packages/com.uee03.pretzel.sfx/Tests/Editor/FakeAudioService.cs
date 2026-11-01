using System.Collections.Generic;
using Pretzel.Audio;
using UnityEngine;

namespace Pretzel.Sfx.Tests
{
    /// <summary>Records what was played. Returns no handle, because handles can only be made by the real service.</summary>
    internal sealed class FakeAudioService : IAudioService
    {
        public readonly List<AudioCue> Cues = new();
        public readonly List<Vector3?> Positions = new();

        public AudioClip CurrentMusic => null;

        public AudioHandle Play(AudioCue cue)
        {
            Cues.Add(cue);
            Positions.Add(null);
            return AudioHandle.None;
        }

        public AudioHandle Play(AudioCue cue, Vector3 position)
        {
            Cues.Add(cue);
            Positions.Add(position);
            return AudioHandle.None;
        }

        public AudioHandle PlayClip(AudioClip clip, AudioChannel channel = AudioChannel.Sfx, float volume = 1f) => AudioHandle.None;

        public bool IsPlaying(AudioHandle handle) => false;

        public void Stop(AudioHandle handle)
        {
        }

        public void StopAll()
        {
        }

        public void PlayMusic(AudioClip clip, float fadeSeconds = -1f, float volume = 1f)
        {
        }

        public void StopMusic(float fadeSeconds = -1f)
        {
        }

        public void SetVolume(AudioChannel channel, float linear)
        {
        }

        public float GetVolume(AudioChannel channel) => 1f;
    }
}
