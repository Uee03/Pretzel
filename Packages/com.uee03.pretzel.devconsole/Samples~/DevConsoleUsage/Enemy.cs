using UnityEngine;

namespace Pretzel.Samples.Console
{
    /// <summary>Chases the player and hits when close. Speed and damage come from <see cref="ArenaTuning"/>, so console changes show at once.</summary>
    public sealed class Enemy : MonoBehaviour
    {
        private const float HitDistance = 1.1f;
        private const float HitInterval = 1f;

        private ArenaGame _game;
        private float _nextHit;

        public void Initialize(ArenaGame game)
        {
            _game = game;
        }

        private void Update()
        {
            if (_game == null || _game.Player.IsDead)
            {
                return;
            }

            Vector3 toPlayer = _game.Player.transform.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            if (distance > HitDistance)
            {
                transform.position += toPlayer / distance * (_game.Tuning.EnemySpeed * Time.deltaTime);
            }
            else if (Time.time >= _nextHit)
            {
                _nextHit = Time.time + HitInterval;
                _game.Player.Damage(_game.Tuning.EnemyDamage);
            }
        }
    }
}
