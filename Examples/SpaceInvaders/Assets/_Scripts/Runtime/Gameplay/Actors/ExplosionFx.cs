using Pretzel.Pooling;
using Pretzel.Tween;
using UnityEngine;

namespace SpaceInvaders.Gameplay
{
    /// <summary>
    /// A pooled burst. It plays the pixel-art explosion frames drawn in Aseprite, tinted per kind of invader, grows a little
    /// with Pretzel easing, then returns itself to the pool.
    /// </summary>
    public sealed class ExplosionFx : MonoBehaviour, IPoolable
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Sprite[] _frames = new Sprite[0];
        [SerializeField, Min(0.05f)] private float _duration = 0.4f;

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
            if (_frames.Length > 0)
            {
                _renderer.sprite = _frames[Mathf.Min(_frames.Length - 1, (int)(progress * _frames.Length))];
            }

            float scale = EaseUtility.Lerp(EaseType.OutCubic, 0.85f, 1.15f, progress) * _size;
            transform.localScale = new Vector3(scale, scale, 1f);
            _renderer.color = _tint;
        }
    }
}
