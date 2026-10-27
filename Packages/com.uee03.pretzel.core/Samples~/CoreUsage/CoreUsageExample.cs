using System;
using System.Collections.Generic;
using Pretzel.Core;
using UnityEngine;

namespace Pretzel.Samples.Core
{
    /// <summary>
    /// Add this to a GameObject in an empty scene and press Play.
    /// Shows the singletons and argument guards.
    /// </summary>
    public class CoreUsageExample : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float _volume = 0.5f;
        [SerializeField] private string _playerName = "Ada";
        [SerializeField] private List<string> _levels = new() { "Tutorial", "Forest", "Castle" };
        [SerializeField] private int _startLevel;

        private void Start()
        {
            SingletonExample();
            GuardExample();
        }

        private void SingletonExample()
        {
            // Instance creates the object when playing if the scene has none.
            GameSession.Instance.AddScore(10);
            Debug.Log($"Score: {GameSession.Instance.Score}");

            // Use HasInstance or TryGetInstance to avoid creating an object, for example inside OnDestroy.
            if (SceneDirector.HasInstance)
            {
                Debug.Log("A SceneDirector already exists.");
            }

            MusicPlayer music = MusicPlayer.TryGetInstance();
            Debug.Log(music ? "Music is playing." : "No MusicPlayer in this scene.");
        }

        private void GuardExample()
        {
            // Guards return the checked value, so they can be used inline. Each throws with the parameter name.
            float volume = Guard.InRange(_volume, 0f, 1f, nameof(_volume));
            string playerName = Guard.NotNullOrEmpty(_playerName, nameof(_playerName));
            Transform self = Guard.NotDestroyed(transform, nameof(transform));
            IReadOnlyCollection<string> levels = Guard.NotNullOrEmpty(_levels, nameof(_levels));
            int startLevel = Guard.InRange(_startLevel, 0, levels.Count - 1, nameof(_startLevel));
            Debug.Log($"{playerName} at volume {volume} on {self.name}, starting on {_levels[startLevel]}.", this);

            // Validate input at the boundary, where you can report it, rather than deep inside your systems.
            try
            {
                ApplyProfile(null);
            }
            catch (ArgumentNullException ex)
            {
                Debug.LogWarning($"Rejected profile: {ex.ParamName} was null.", this);
            }
        }

        private static void ApplyProfile(string profileJson)
        {
            Guard.NotNull(profileJson, nameof(profileJson));
        }
    }
}
