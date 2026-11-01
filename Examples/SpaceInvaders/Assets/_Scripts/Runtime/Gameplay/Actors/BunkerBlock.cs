using UnityEngine;

namespace SpaceInvaders.Gameplay
{
    /// <summary>One brick of a bunker. Absorbs a few hits from either side and fades as it wears down.</summary>
    public sealed class BunkerBlock : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] private int _maxHealth = 3;
        [SerializeField] private SpriteRenderer _renderer;

        private Color _baseColor;
        private int _health;

        private void Awake()
        {
            _baseColor = _renderer.color;
            _health = _maxHealth;
        }

        public bool TakeHit(Team attacker)
        {
            if (_health <= 0)
            {
                return false;
            }

            _health--;
            if (_health <= 0)
            {
                gameObject.SetActive(false);
                return true;
            }

            ApplyWear();
            return true;
        }

        public void Restore()
        {
            _health = _maxHealth;
            gameObject.SetActive(true);
            ApplyWear();
        }

        private void ApplyWear()
        {
            float health = _health / (float)_maxHealth;
            _renderer.color = Color.Lerp(new Color(_baseColor.r * 0.35f, _baseColor.g * 0.35f, _baseColor.b * 0.35f, _baseColor.a), _baseColor, health);
        }
    }
}
