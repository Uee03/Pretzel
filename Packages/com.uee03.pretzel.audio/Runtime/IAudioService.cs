using UnityEngine;

namespace Pretzel.Audio
{
    /// <summary>
    /// What game code needs from an audio system. <see cref="AudioService"/> is the implementation; depend on this interface in
    /// gameplay code so tests can pass a fake, and so another audio backend can replace the service without touching callers.
    /// </summary>
    public interface IAudioService
    {
        /// <summary>The clip of the music that plays now, or null.</summary>
        AudioClip CurrentMusic { get; }

        /// <summary>Plays a cue as 2D, or as the cue sets it. Returns <see cref="AudioHandle.None"/> when nothing could play.</summary>
        AudioHandle Play(AudioCue cue);

        /// <summary>Plays a cue at a world position.</summary>
        AudioHandle Play(AudioCue cue, Vector3 position);

        AudioHandle PlayClip(AudioClip clip, AudioChannel channel = AudioChannel.Sfx, float volume = 1f);

        bool IsPlaying(AudioHandle handle);

        /// <summary>Stops one sound. A stale handle does nothing.</summary>
        void Stop(AudioHandle handle);

        void StopAll();

        /// <summary>Crossfades to a clip. Playing the clip that already plays only updates its volume.</summary>
        /// <param name="fadeSeconds">Unscaled seconds. Negative uses the default fade.</param>
        void PlayMusic(AudioClip clip, float fadeSeconds = -1f, float volume = 1f);

        void StopMusic(float fadeSeconds = -1f);

        /// <param name="linear">0 to 1.</param>
        void SetVolume(AudioChannel channel, float linear);

        float GetVolume(AudioChannel channel);
    }
}
