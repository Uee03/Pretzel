using System;
using UnityEngine;

namespace SpaceInvaders.Gameplay
{
    /// <summary>The mystery ship. Flies across the top once launched and can only be hit by the player.</summary>
    public sealed class Ufo : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(0f)] private float _speed = 3.5f;
        [SerializeField] private float _limitX = 9.5f;

        private float _direction;

        public bool IsFlying => _direction != 0f;

        public event Action<Ufo> Killed;

        public event Action<Ufo> Escaped;

        public void Launch(float y, bool fromLeft)
        {
            _direction = fromLeft ? 1f : -1f;
            transform.position = new Vector3(fromLeft ? -_limitX : _limitX, y, 0f);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _direction = 0f;
            gameObject.SetActive(false);
        }

        /// <summary>Shows or hides the ship where the host says it is. A mirrored ship never flies by itself.</summary>
        public void Mirror(bool visible, Vector2 position)
        {
            if (visible)
            {
                transform.position = new Vector3(position.x, position.y, 0f);
            }

            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }

        private void Update()
        {
            if (!IsFlying)
            {
                return;
            }

            transform.position += new Vector3(_direction * _speed * Time.deltaTime, 0f, 0f);
            if (Mathf.Abs(transform.position.x) > _limitX + 0.1f && Mathf.Sign(transform.position.x) == Mathf.Sign(_direction))
            {
                Hide();
                Escaped?.Invoke(this);
            }
        }

        public bool TakeHit(Team attacker)
        {
            if (!IsFlying || attacker != Team.Player)
            {
                return false;
            }

            Killed?.Invoke(this);
            Hide();
            return true;
        }
    }
}
