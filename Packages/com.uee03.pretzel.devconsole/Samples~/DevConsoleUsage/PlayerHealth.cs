using System;
using UnityEngine;

namespace Pretzel.Samples.Console
{
    /// <summary>The player's hit points. The console's god command sets <see cref="IsGodMode"/>, which <see cref="Damage"/> honors.</summary>
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _max = 100f;

        private float _current;

        public float Current => _current;

        public float Max => _max;

        public bool IsGodMode { get; set; }

        public bool IsDead => _current <= 0f;

        public event Action Died;

        private void Awake()
        {
            _current = _max;
        }

        public void Damage(float amount)
        {
            if (IsGodMode || IsDead || amount <= 0f)
            {
                return;
            }

            _current = Mathf.Max(0f, _current - amount);
            if (_current <= 0f)
            {
                Died?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (!IsDead && amount > 0f)
            {
                _current = Mathf.Min(_max, _current + amount);
            }
        }

        public void Revive()
        {
            _current = _max;
        }
    }
}
