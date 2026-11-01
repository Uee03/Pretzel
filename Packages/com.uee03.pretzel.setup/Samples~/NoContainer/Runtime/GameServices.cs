using Pretzel.Data;
using Pretzel.Events;
using Pretzel.Preferences;
using Pretzel.Scenes;
using UnityEngine;

namespace Pretzel.Samples.NoContainer
{
    /// <summary>
    /// The game's shared services, held in one asset instead of a container. <see cref="GameServicesBootstrap"/> calls
    /// <see cref="Initialize"/> before the first scene loads, so every scene can use the asset from Awake.
    /// The services are runtime state, not serialized data: they are rebuilt every session and never saved in the asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Pretzel/Samples/No Container/Game Services", fileName = "GameServices")]
    public sealed class GameServices : ScriptableObject
    {
        public const string ResourceName = "GameServices";

        [SerializeField, Min(1f)] private float _roundSeconds = 15f;

        private EventBus _bus;

        public float RoundSeconds => _roundSeconds;

        public IEventBus Events => _bus;

        public SceneLoader Scenes { get; private set; }

        public IProgressService Progress { get; private set; }

        public IPreferencesService Preferences { get; private set; }

        public bool IsInitialized => _bus != null;

        /// <summary>Builds every service in dependency order. Safe to call again: the previous session's services are shut down first.</summary>
        public void Initialize(IDataStore store = null)
        {
            Shutdown();

            IDataStore dataStore = store ?? DataIO.Default;
            _bus = new EventBus();
            Scenes = new SceneLoader();

            Preferences = new PreferencesService(dataStore);
            Preferences.Load();
            Preferences.Changed += ApplyAudio;
            ApplyAudio(Preferences.Current);

            Progress = new ProgressService(dataStore);
            Progress.Load();
        }

        /// <summary>Saves what is pending and drops every service, so nothing from a finished session stays reachable.</summary>
        public void Shutdown()
        {
            if (!IsInitialized)
            {
                return;
            }

            Progress.Save();
            Preferences.Save();
            Preferences.Changed -= ApplyAudio;
            _bus.Clear();

            _bus = null;
            Scenes = null;
            Progress = null;
            Preferences = null;
        }

        private static void ApplyAudio(PlayerPreferences preferences)
        {
            AudioListener.volume = preferences.audio.muted ? 0f : preferences.audio.master;
        }
    }
}
