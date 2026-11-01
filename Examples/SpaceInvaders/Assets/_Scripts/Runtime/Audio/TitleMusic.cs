using Pretzel.Audio;
using UnityEngine;
using VContainer.Unity;

namespace SpaceInvaders.Audio
{
    /// <summary>Plays the title tune when the title scene starts. It crossfades from whatever played before, such as a lost game's silence.</summary>
    public sealed class TitleMusic : IStartable
    {
        public const float Volume = 0.55f;
        public const float FadeSeconds = 1f;

        private readonly IAudioService _audio;
        private readonly SoundBank _sounds;

        public TitleMusic(IAudioService audio, SoundBank sounds)
        {
            _audio = audio;
            _sounds = sounds;
        }

        public void Start()
        {
            AudioClip clip = _sounds.TitleMusic;
            if (clip != null)
            {
                _audio.PlayMusic(clip, FadeSeconds, Volume);
            }
        }
    }
}
