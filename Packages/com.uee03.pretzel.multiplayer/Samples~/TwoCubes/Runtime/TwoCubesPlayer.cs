using Unity.Netcode;
using UnityEngine;

namespace Pretzel.Multiplayer.Samples.TwoCubes
{
    /// <summary>
    /// Owner-authoritative cube. Only the client that controls it reads input and moves it; NetworkTransform (authority mode Owner, interpolation on)
    /// sends the result to everyone else. The cube can only move, jump and turn. Its colour follows the slot the host assigned.
    /// </summary>
    [RequireComponent(typeof(NetworkPlayerIdentity))]
    [DisallowMultipleComponent]
    public sealed class TwoCubesPlayer : NetworkBehaviour
    {
        private static readonly Color[] _palette =
        {
            new Color(0.90f, 0.30f, 0.30f),
            new Color(0.30f, 0.55f, 0.95f),
            new Color(0.35f, 0.80f, 0.45f),
            new Color(0.95f, 0.80f, 0.25f),
            new Color(0.80f, 0.40f, 0.85f),
            new Color(0.30f, 0.85f, 0.85f),
            new Color(0.95f, 0.60f, 0.25f),
            new Color(0.85f, 0.85f, 0.85f),
        };

        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int _colorId = Shader.PropertyToID("_Color");
        private static readonly ITwoCubesInput _sharedKeyboard = new TwoCubesKeyboardInput();

        [SerializeField] private TwoCubesPlane _plane = TwoCubesPlane.Horizontal3D;
        [SerializeField, Min(0f)] private float _moveSpeed = 5f;
        [SerializeField, Min(0f)] private float _jumpSpeed = 7f;
        [SerializeField, Min(0.1f)] private float _groundY = 0.5f;

        private readonly TwoCubesMotor _motor = new TwoCubesMotor();
        private NetworkPlayerIdentity _identity;
        private Renderer _renderer;
        private SpriteRenderer _sprite;
        private MaterialPropertyBlock _block;
        private bool _wasControlled;

        /// <summary>Overrides the keyboard, for tests or gamepad support.</summary>
        public ITwoCubesInput Input { get; set; }

        public TwoCubesMotor Motor => _motor;

        private void Awake()
        {
            _identity = GetComponent<NetworkPlayerIdentity>();
            _renderer = GetComponentInChildren<Renderer>();
            _sprite = _renderer as SpriteRenderer;
            _block = new MaterialPropertyBlock();
            _motor.MoveSpeed = _moveSpeed;
            _motor.JumpSpeed = _jumpSpeed;
            _motor.GroundY = _groundY;
        }

        public override void OnNetworkSpawn()
        {
            _identity.IdentityChanged += OnIdentityChanged;
            OnIdentityChanged(_identity);
        }

        public override void OnNetworkDespawn()
        {
            _identity.IdentityChanged -= OnIdentityChanged;
            _wasControlled = false;
        }

        private void Update()
        {
            if (!_identity.IsLocallyControlled)
            {
                return;
            }

            int index = _identity.LocalPlayerIndex;
            ITwoCubesInput source = Input ?? _sharedKeyboard;
            if (_motor.Step(_plane, source.ReadMove(index), source.ReadJump(index), Time.deltaTime))
            {
                transform.SetPositionAndRotation(_motor.Position, Quaternion.Euler(0f, _motor.Yaw, 0f));
            }
        }

        private void OnIdentityChanged(NetworkPlayerIdentity identity)
        {
            ApplyColor(identity.SlotIndex);
            bool controlled = identity.IsLocallyControlled;
            if (controlled && !_wasControlled)
            {
                // Control was just handed to this device (first spawn or a rejoin): continue from where the object is.
                _motor.SetState(transform.position, transform.eulerAngles.y);
            }

            _wasControlled = controlled;
        }

        private void ApplyColor(int slot)
        {
            Color color = _palette[slot % _palette.Length];
            if (_sprite != null)
            {
                _sprite.color = color;
            }
            else if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_block);
                _block.SetColor(_baseColorId, color);
                _block.SetColor(_colorId, color);
                _renderer.SetPropertyBlock(_block);
            }
        }
    }
}
