using Pretzel.Pooling;
using Unity.Netcode;
using UnityEngine;

namespace Pretzel.Multiplayer.Samples.TwoCubes
{
    /// <summary>
    /// A server-spawned item that comes from the pooled spawn handler. The server picks a colour in OnNetworkSpawn (NetworkVariable values are not reset by the pool,
    /// so every spawn writes them again); local state is reset in <see cref="OnTakenFromPool"/>.
    /// </summary>
    public sealed class TwoCubesItem : NetworkBehaviour, IPoolable
    {
        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int _colorId = Shader.PropertyToID("_Color");

        private readonly NetworkVariable<byte> _colorIndex = new NetworkVariable<byte>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        [SerializeField, Min(0f)] private float _spinDegreesPerSecond = 90f;

        private Renderer _renderer;
        private MaterialPropertyBlock _block;
        private float _angle;

        private void Awake()
        {
            _renderer = GetComponentInChildren<Renderer>();
            _block = new MaterialPropertyBlock();
        }

        public void OnTakenFromPool()
        {
            _angle = 0f;
        }

        public void OnReturnedToPool()
        {
        }

        public override void OnNetworkSpawn()
        {
            _colorIndex.OnValueChanged += OnColorChanged;
            if (IsServer)
            {
                _colorIndex.Value = (byte)Random.Range(0, 8);
            }

            ApplyColor(_colorIndex.Value);
        }

        public override void OnNetworkDespawn()
        {
            _colorIndex.OnValueChanged -= OnColorChanged;
        }

        private void Update()
        {
            _angle += _spinDegreesPerSecond * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0f, _angle, 0f);
        }

        private void OnColorChanged(byte previous, byte current)
        {
            ApplyColor(current);
        }

        private void ApplyColor(int index)
        {
            if (_renderer == null)
            {
                return;
            }

            Color color = Color.HSVToRGB((index % 8) / 8f, 0.65f, 0.95f);
            SpriteRenderer sprite = _renderer as SpriteRenderer;
            if (sprite != null)
            {
                sprite.color = color;
                return;
            }

            _renderer.GetPropertyBlock(_block);
            _block.SetColor(_baseColorId, color);
            _block.SetColor(_colorId, color);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
