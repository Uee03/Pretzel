using Pretzel.Core;
using Pretzel.Preferences;
using UnityEngine;
using UnityEngine.Audio;

namespace Pretzel.Samples.Preferences
{
    /// <summary>Writes the saved volumes to exposed AudioMixer parameters, in decibels. Empty parameter names are skipped.</summary>
    public sealed class AudioMixerApplier : IPreferenceApplier
    {
        private readonly AudioMixer _mixer;
        private readonly string _masterParameter;
        private readonly string _musicParameter;
        private readonly string _sfxParameter;

        public AudioMixerApplier(AudioMixer mixer, string masterParameter, string musicParameter, string sfxParameter)
        {
            _mixer = Guard.NotNull(mixer, nameof(mixer));
            _masterParameter = masterParameter;
            _musicParameter = musicParameter;
            _sfxParameter = sfxParameter;
        }

        public void Apply(PlayerPreferences preferences)
        {
            AudioPreferences audio = preferences.audio;
            Set(_masterParameter, audio.muted ? 0f : audio.master);
            Set(_musicParameter, audio.music);
            Set(_sfxParameter, audio.sfx);
        }

        private void Set(string parameter, float linear)
        {
            if (string.IsNullOrEmpty(parameter))
            {
                return;
            }

            float decibels = linear <= 0.0001f ? -80f : 20f * Mathf.Log10(linear);
            if (!_mixer.SetFloat(parameter, decibels))
            {
                Debug.LogWarning($"AudioMixer '{_mixer.name}' has no exposed parameter '{parameter}'.");
            }
        }
    }
}
