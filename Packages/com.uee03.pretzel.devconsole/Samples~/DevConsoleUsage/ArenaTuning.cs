using System;
using System.Collections.Generic;

namespace Pretzel.Samples.Console
{
    /// <summary>
    /// Gameplay numbers that the game reads every frame and the console can change. Exposing tuning as named values gives
    /// you generic <c>get</c>, <c>set</c> and <c>tuning</c> commands without writing one command per number.
    /// </summary>
    public sealed class ArenaTuning
    {
        private readonly Dictionary<string, (Func<float> Get, Action<float> Set)> _values = new(StringComparer.OrdinalIgnoreCase);

        public ArenaTuning()
        {
            Add("playerSpeed", () => PlayerSpeed, v => PlayerSpeed = v);
            Add("enemySpeed", () => EnemySpeed, v => EnemySpeed = v);
            Add("enemyDamage", () => EnemyDamage, v => EnemyDamage = v);
            Add("auraRadius", () => AuraRadius, v => AuraRadius = v);
            Add("goldPerKill", () => GoldPerKill, v => GoldPerKill = v);
            Add("spawnInterval", () => SpawnInterval, v => SpawnInterval = v);
            Add("maxEnemies", () => MaxEnemies, v => MaxEnemies = v);
        }

        public float PlayerSpeed { get; set; } = 6f;

        public float EnemySpeed { get; set; } = 2.5f;

        public float EnemyDamage { get; set; } = 5f;

        public float AuraRadius { get; set; } = 2.5f;

        public float GoldPerKill { get; set; } = 10f;

        /// <summary>Seconds between automatic spawns. Zero or less turns automatic spawning off.</summary>
        public float SpawnInterval { get; set; } = 3f;

        public float MaxEnemies { get; set; } = 30f;

        public IEnumerable<string> Names => _values.Keys;

        public bool TryGet(string name, out float value)
        {
            if (_values.TryGetValue(name, out (Func<float> Get, Action<float> Set) entry))
            {
                value = entry.Get();
                return true;
            }

            value = 0f;
            return false;
        }

        public bool TrySet(string name, float value)
        {
            if (!_values.TryGetValue(name, out (Func<float> Get, Action<float> Set) entry))
            {
                return false;
            }

            entry.Set(value);
            return true;
        }

        private void Add(string name, Func<float> get, Action<float> set) => _values.Add(name, (get, set));
    }
}
