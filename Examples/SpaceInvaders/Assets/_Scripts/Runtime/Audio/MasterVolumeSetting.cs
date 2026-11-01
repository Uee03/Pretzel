using Pretzel.Preferences;
using UnityEngine;

namespace SpaceInvaders.Audio
{
    /// <summary>The master volume the player sets in the menus. Stored in Pretzel Preferences and applied to the audio service by the game flow.</summary>
    public sealed class MasterVolumeSetting
    {
        private readonly IPreferencesService _preferences;

        public MasterVolumeSetting(IPreferencesService preferences)
        {
            _preferences = preferences;
        }

        public float Value => _preferences.Current.audio.master;

        public void Set(float volume)
        {
            _preferences.Modify(p => p.audio.master = Mathf.Clamp01(volume), save: true);
        }
    }
}
