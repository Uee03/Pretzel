using System.Collections.Generic;
using Pretzel.Audio;
using UnityEngine;

namespace SpaceInvaders.Audio
{
    /// <summary>Every sound the game plays, in one asset. Registered as a scope setting, so classes ask for it in their constructor.</summary>
    [CreateAssetMenu(menuName = "Space Invaders/Sound Bank", fileName = "SoundBank")]
    public sealed class SoundBank : ScriptableObject
    {
        [SerializeField] private AudioCue _playerShot;
        [SerializeField] private AudioCue _invaderDestroyed;
        [SerializeField] private AudioCue _playerHit;
        [SerializeField] private AudioCue _ufoDestroyed;
        [SerializeField] private AudioCue _waveCleared;
        [SerializeField] private AudioCue _gameOver;

        [Tooltip("The four notes of the march, played in order on every step.")]
        [SerializeField] private AudioClip[] _march = new AudioClip[0];

        public AudioCue PlayerShot => _playerShot;

        public AudioCue InvaderDestroyed => _invaderDestroyed;

        public AudioCue PlayerHit => _playerHit;

        public AudioCue UfoDestroyed => _ufoDestroyed;

        public AudioCue WaveCleared => _waveCleared;

        public AudioCue GameOver => _gameOver;

        public IReadOnlyList<AudioClip> March => _march;

        public AudioClip MarchNote(int stepIndex)
        {
            return _march.Length == 0 ? null : _march[stepIndex % _march.Length];
        }
    }
}
