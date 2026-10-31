using System;
using System.Collections.Generic;
using Pretzel.Data;
using Pretzel.Preferences;
using UnityEngine;
using UnityEngine.Audio;

namespace Pretzel.Samples.Preferences
{
    /// <summary>
    /// Wires a complete settings flow: loads the preferences, runs every <see cref="IPreferenceApplier"/> on load and on each
    /// change, and hands the service to the options menu.
    /// </summary>
    public sealed class OptionsMenuSetup : MonoBehaviour
    {
        [SerializeField] private OptionsMenuController _menu;

        [Header("Optional AudioMixer")]
        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private string _masterParameter = "MasterVolume";
        [SerializeField] private string _musicParameter = "MusicVolume";
        [SerializeField] private string _sfxParameter = "SfxVolume";

        private readonly List<IPreferenceApplier> _appliers = new();
        private IPreferencesService _preferences;

        /// <summary>Connect a localization system to this.</summary>
        public LanguageApplier Language { get; } = new();

        private async void Start()
        {
            _preferences = new PreferencesService();
            _appliers.Add(_mixer ? new AudioMixerApplier(_mixer, _masterParameter, _musicParameter, _sfxParameter) : new AudioListenerApplier());
            _appliers.Add(Language);
            _preferences.Changed += ApplyAll;
            try
            {
                LoadResult<PlayerPreferences> result = await _preferences.LoadAsync(destroyCancellationToken);
                if (result.Status == DataStatus.Corrupt)
                {
                    Debug.LogWarning($"Preferences could not be read ({result.Error}). Using defaults.", this);
                }

                _preferences.ApplyGraphics();
                if (_menu)
                {
                    _menu.Bind(_preferences);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void OnDestroy()
        {
            if (_preferences != null)
            {
                _preferences.Changed -= ApplyAll;
            }
        }

        private void ApplyAll(PlayerPreferences preferences)
        {
            foreach (IPreferenceApplier applier in _appliers)
            {
                applier.Apply(preferences);
            }
        }
    }
}
