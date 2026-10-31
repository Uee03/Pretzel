using System;
using Pretzel.Data;
using Pretzel.Preferences;
using UnityEngine;

namespace Pretzel.Samples.Preferences
{
    /// <summary>
    /// The composition root for preferences: the only place that knows which <see cref="IPreferencesService"/> is used.
    /// Everything else receives the interface through <see cref="Ready"/> or <see cref="Preferences"/>.
    /// </summary>
    public sealed class PreferencesBootstrap : MonoBehaviour
    {
        [SerializeField] private PreferencesBackend _backend = PreferencesBackend.PretzelData;

        public IPreferencesService Preferences { get; private set; }

        /// <summary>Raised once the preferences are loaded. Subscribe in Awake; it fires from Start.</summary>
        public event Action<IPreferencesService> Ready;

        private async void Start()
        {
            Preferences = Create(_backend);
            try
            {
                LoadResult<PlayerPreferences> result = await Preferences.LoadAsync(destroyCancellationToken);
                if (result.Status == DataStatus.Corrupt)
                {
                    Debug.LogWarning($"Preferences could not be read ({result.Error}). Using defaults.", this);
                }

                Ready?.Invoke(Preferences);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private static IPreferencesService Create(PreferencesBackend backend)
        {
            switch (backend)
            {
                case PreferencesBackend.PlayerPrefs:
                    return new PlayerPrefsPreferencesService();
                case PreferencesBackend.InMemory:
                    return new InMemoryPreferencesService();
                default:
                    return new PreferencesService();
            }
        }
    }
}
