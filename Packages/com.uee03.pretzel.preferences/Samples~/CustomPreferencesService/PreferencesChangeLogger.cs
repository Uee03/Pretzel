using Pretzel.Preferences;
using UnityEngine;

namespace Pretzel.Samples.Preferences
{
    /// <summary>
    /// A consumer that only knows <see cref="IPreferencesService"/>. It logs every change, so you can see that swapping the
    /// backend on <see cref="PreferencesBootstrap"/> needs no change here.
    /// </summary>
    [RequireComponent(typeof(PreferencesBootstrap))]
    public sealed class PreferencesChangeLogger : MonoBehaviour
    {
        private PreferencesBootstrap _bootstrap;
        private IPreferencesService _preferences;

        private void Awake()
        {
            _bootstrap = GetComponent<PreferencesBootstrap>();
            _bootstrap.Ready += OnReady;
        }

        private void OnDestroy()
        {
            _bootstrap.Ready -= OnReady;
            if (_preferences != null)
            {
                _preferences.Changed -= OnChanged;
            }
        }

        [ContextMenu("Toggle Mute And Save")]
        private void ToggleMute()
        {
            _bootstrap.Preferences?.Modify(p => p.audio.muted = !p.audio.muted, save: true);
        }

        private void OnReady(IPreferencesService preferences)
        {
            _preferences = preferences;
            _preferences.Changed += OnChanged;
            OnChanged(preferences.Current);
        }

        private void OnChanged(PlayerPreferences preferences)
        {
            Debug.Log($"Preferences: master {preferences.audio.master:0.00}, music {preferences.audio.music:0.00}, muted {preferences.audio.muted}, language '{preferences.language}'.", this);
        }
    }
}
