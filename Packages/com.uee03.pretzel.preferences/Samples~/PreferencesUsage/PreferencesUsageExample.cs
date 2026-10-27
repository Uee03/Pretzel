using System;
using Pretzel.Data;
using Pretzel.Preferences;
using UnityEngine;
using UnityEngine.Audio;

namespace Pretzel.Samples.Preferences
{
    /// <summary>
    /// Loads preferences at startup, applies graphics, and keeps an AudioMixer in sync with the saved volumes.
    /// An options menu calls <see cref="Service"/>.Modify(..., save: true) when the player changes a setting.
    /// </summary>
    public sealed class PreferencesUsageExample : MonoBehaviour
    {
        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private string _masterParameter = "MasterVolume";
        [SerializeField] private string _musicParameter = "MusicVolume";
        [SerializeField] private string _sfxParameter = "SfxVolume";

        public PreferencesService Service { get; } = new();

        private void Awake()
        {
            Service.Changed += Apply;
        }

        private async void Start()
        {
            try
            {
                LoadResult<PlayerPreferences> result = await Service.LoadAsync(destroyCancellationToken);
                if (result.Status == DataStatus.Corrupt)
                {
                    Debug.LogWarning($"Preferences could not be read ({result.Error}). Using defaults.", this);
                }

                Service.ApplyGraphics();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void OnDestroy()
        {
            Service.Changed -= Apply;
        }

        [ContextMenu("Lower Music And Save")]
        public void LowerMusic() => Service.Modify(p => p.audio.music = Mathf.Max(0f, p.audio.music - 0.1f), save: true);

        [ContextMenu("Reset To Defaults")]
        public void ResetAll() => Service.ResetToDefaults(save: true);

        private void Apply(PlayerPreferences preferences)
        {
            if (!_mixer)
            {
                return;
            }

            float master = preferences.audio.muted ? 0f : preferences.audio.master;
            _mixer.SetFloat(_masterParameter, ToDecibels(master));
            _mixer.SetFloat(_musicParameter, ToDecibels(preferences.audio.music));
            _mixer.SetFloat(_sfxParameter, ToDecibels(preferences.audio.sfx));
        }

        private static float ToDecibels(float linear) => linear <= 0.0001f ? -80f : 20f * Mathf.Log10(linear);
    }
}
