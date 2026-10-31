using Pretzel.Preferences;
using UnityEngine;

namespace Pretzel.Samples.Preferences
{
    /// <summary>The simplest audio applier: scales everything the AudioListener hears. Use it when you have no AudioMixer.</summary>
    public sealed class AudioListenerApplier : IPreferenceApplier
    {
        public void Apply(PlayerPreferences preferences)
        {
            AudioListener.volume = preferences.audio.muted ? 0f : preferences.audio.master;
        }
    }
}
