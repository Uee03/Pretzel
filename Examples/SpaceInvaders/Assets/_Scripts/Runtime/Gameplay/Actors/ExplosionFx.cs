using Pretzel.Pooling;
using Pretzel.Tween;
using UnityEngine;

namespace SpaceInvaders.Gameplay
{
    /// <summary>A pooled burst: grows and fades with Pretzel easing, then returns itself to the pool.</summary>
    public sealed class ExplosionFx : MonoBehaviour, IPoolable
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField, Min(0.05f)] private float _duration = 0.35f;

        private PooledObject _pooled;
        private Color _tint = Color.white;
        private float _size = 1f;
        private float _age;

        private void Awake()
        {
            _pooled = GetComponent<PooledObject>();
        }

        public void OnTakenFromPool()
        {
            _age = 0f;
            Apply(0f);
        }

        public void OnReturnedToPool()
        {
        }

        public void Play(Color tint, float size)
        {
            _tint = tint;
            _size = size;
            _age = 0f;
            Apply(0f);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float progress = Mathf.Clamp01(_age / _duration);
            Apply(progress);
            if (progress >= 1f)
            {
                _pooled.Release();
            }
        }

        private void Apply(float progress)
        {
            float scale = EaseUtility.Lerp(EaseType.OutCubic, 0.2f, _size, progress);
            transform.localScale = new Vector3(scale, scale, 1f);
            Color color = _tint;
            color.a = 1f - EaseUtility.Evaluate(EaseType.InQuad, progress);
            _renderer.color = color;
        }
    }
}
