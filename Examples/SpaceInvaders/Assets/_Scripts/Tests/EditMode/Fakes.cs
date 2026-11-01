using System.Collections.Generic;
using Pretzel.Audio;
using Pretzel.Cameras;
using Pretzel.Sfx;
using Pretzel.Vfx;
using UnityEngine;

namespace SpaceInvaders.Tests
{
    /// <summary>Records every freeze and slow motion the game feel asks for.</summary>
    internal sealed class FakeTimeScale : ITimeScale
    {
        public readonly List<float> Freezes = new();
        public readonly List<(float Scale, float Seconds)> SlowMotions = new();

        public int Freeze(float seconds)
        {
            Freezes.Add(seconds);
            return Freezes.Count;
        }

        public int SlowMotion(float scale, float seconds)
        {
            SlowMotions.Add((scale, seconds));
            return SlowMotions.Count;
        }

        public bool Cancel(int id) => false;
    }

    internal sealed class FakeShaker : ICameraShaker
    {
        public readonly List<float> Hits = new();

        public float Trauma { get; private set; }

        public void AddTrauma(float amount)
        {
            Hits.Add(amount);
            Trauma = Mathf.Clamp01(Trauma + amount);
        }
    }

    internal sealed class FakeSfx : ISfxPlayer
    {
        public readonly List<string> Played = new();

        public AudioHandle Play(string id)
        {
            Played.Add(id);
            return AudioHandle.None;
        }

        public AudioHandle Play(string id, Vector3 position) => Play(id);

        public void Stop(string id)
        {
        }
    }

    internal sealed class FakeAudio : IAudioService
    {
        public readonly List<AudioClip> Clips = new();
        public readonly List<AudioClip> Music = new();
        public int MusicStops;

        public AudioClip CurrentMusic => Music.Count == 0 ? null : Music[Music.Count - 1];

        public AudioHandle Play(AudioCue cue) => AudioHandle.None;

        public AudioHandle Play(AudioCue cue, Vector3 position) => AudioHandle.None;

        public AudioHandle PlayClip(AudioClip clip, AudioChannel channel = AudioChannel.Sfx, float volume = 1f)
        {
            Clips.Add(clip);
            return AudioHandle.None;
        }

        public bool IsPlaying(AudioHandle handle) => false;

        public void Stop(AudioHandle handle)
        {
        }

        public void StopAll()
        {
        }

        public void PlayMusic(AudioClip clip, float fadeSeconds = -1f, float volume = 1f)
        {
            Music.Add(clip);
        }

        public void StopMusic(float fadeSeconds = -1f)
        {
            MusicStops++;
        }

        public void SetVolume(AudioChannel channel, float linear)
        {
        }

        public float GetVolume(AudioChannel channel) => 1f;
    }
}
