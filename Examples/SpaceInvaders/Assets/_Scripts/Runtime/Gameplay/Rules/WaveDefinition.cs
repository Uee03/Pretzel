using System.Collections.Generic;
using UnityEngine;

namespace SpaceInvaders.Gameplay
{
    /// <summary>One wave of invaders: the formation, how fast it marches and how often it bombs. Authored in the Inspector.</summary>
    [CreateAssetMenu(menuName = "Space Invaders/Wave Definition", fileName = "Wave")]
    public sealed class WaveDefinition : ScriptableObject
    {
        [SerializeField] private string _title = "Wave";
        [SerializeField, Range(3, 12)] private int _columns = 11;

        [Tooltip("One entry per row, top row first.")]
        [SerializeField] private InvaderKind[] _rows = { InvaderKind.Squid, InvaderKind.Crab, InvaderKind.Crab, InvaderKind.Octopus, InvaderKind.Octopus };

        [SerializeField] private Vector2 _spacing = new(1f, 0.8f);

        [Header("Tempo")]
        [Tooltip("Seconds between steps with a full formation.")]
        [SerializeField, Min(0.05f)] private float _slowestStepSeconds = 0.9f;

        [Tooltip("Seconds between steps with a single invader left.")]
        [SerializeField, Min(0.02f)] private float _fastestStepSeconds = 0.08f;

        [Header("Bombs")]
        [SerializeField, Min(0.1f)] private float _bombIntervalSeconds = 1.2f;
        [SerializeField, Range(1, 8)] private int _maxBombs = 3;

        public string Title => _title;

        public int Columns => _columns;

        public IReadOnlyList<InvaderKind> Rows => _rows;

        public Vector2 Spacing => _spacing;

        public float SlowestStepSeconds => _slowestStepSeconds;

        public float FastestStepSeconds => Mathf.Min(_fastestStepSeconds, _slowestStepSeconds);

        public float BombIntervalSeconds => _bombIntervalSeconds;

        public int MaxBombs => _maxBombs;

        public int InvaderCount => _columns * _rows.Length;

        public int TotalPoints()
        {
            int points = 0;
            foreach (InvaderKind kind in _rows)
            {
                points += ScoreTable.PointsFor(kind) * _columns;
            }

            return points;
        }
    }
}
