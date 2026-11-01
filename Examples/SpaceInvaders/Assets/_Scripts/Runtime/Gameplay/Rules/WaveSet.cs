using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceInvaders.Gameplay
{
    /// <summary>The waves of a game in order. After the last one the set starts again, with the formation starting lower each lap.</summary>
    [CreateAssetMenu(menuName = "Space Invaders/Wave Set", fileName = "WaveSet")]
    public sealed class WaveSet : ScriptableObject
    {
        public const int MaxDropRows = 4;

        [SerializeField] private WaveDefinition[] _waves = new WaveDefinition[0];

        public IReadOnlyList<WaveDefinition> Waves => _waves;

        /// <param name="waveNumber">Starts at 1.</param>
        public WaveDefinition GetWave(int waveNumber)
        {
            if (_waves == null || _waves.Length == 0)
            {
                throw new InvalidOperationException($"{name} has no waves.");
            }

            return _waves[(Math.Max(1, waveNumber) - 1) % _waves.Length];
        }

        public int StartDropRows(int waveNumber) => StartDropRows(waveNumber, _waves.Length);

        public static int StartDropRows(int waveNumber, int waveCount)
        {
            if (waveCount <= 0)
            {
                return 0;
            }

            return Math.Min((Math.Max(1, waveNumber) - 1) / waveCount, MaxDropRows);
        }
    }
}
