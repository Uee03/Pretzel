using System.Collections.Generic;
using UnityEngine;

namespace SpaceInvaders.Audio
{
    /// <summary>
    /// The four notes of the march and the three tunes. The other sounds live in the SfxLibrary, by id (<see cref="GameSounds"/>). Registered as a
    /// scope setting, so classes ask for it in their constructor.
    /// </summary>
    [CreateAssetMenu(menuName = "Space Invaders/Sound Bank", fileName = "SoundBank")]
    public sealed class SoundBank : ScriptableObject
    {
        [Tooltip("The four notes of the march, played in order on every step.")]
        [SerializeField] private AudioClip[] _march = new AudioClip[0];

        [Header("Music")]
        [SerializeField] private AudioClip _titleMusic;
        [SerializeField] private AudioClip _gameplayMusic;
        [SerializeField] private AudioClip _lastStandMusic;

        /// <summary>The first wave that plays the last-stand tune.</summary>
        public const int LastStandFromWave = 5;

        public IReadOnlyList<AudioClip> March => _march;

        public AudioClip TitleMusic => _titleMusic;

        public AudioClip GameplayMusic => _gameplayMusic;

        public AudioClip LastStandMusic => _lastStandMusic;

        /// <summary>The tune for a wave: the gameplay loop at first, the last stand from wave 5 on. Falls back to the other if one is missing.</summary>
        public AudioClip MusicForWave(int wave)
        {
            if (wave >= LastStandFromWave && _lastStandMusic != null)
            {
                return _lastStandMusic;
            }

            return _gameplayMusic != null ? _gameplayMusic : _lastStandMusic;
        }

        public AudioClip MarchNote(int stepIndex)
        {
            return _march.Length == 0 ? null : _march[stepIndex % _march.Length];
        }
    }
}
