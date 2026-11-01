using UnityEngine;
using UnityEngine.InputSystem;

namespace Pretzel.Inputs.Samples
{
    /// <summary>
    /// A square that moves with the Move action and jumps with a buffered Jump: a press a few frames before landing still
    /// jumps when the square lands. It reads actions only, so pausing (which disables the Gameplay map) stops it.
    /// </summary>
    public sealed class ExamplePlayer : MonoBehaviour
    {
        [SerializeField] private InputExample _input;
        [SerializeField, Min(0f)] private float _speed = 6f;
        [SerializeField, Min(0f)] private float _jumpSpeed = 9f;
        [SerializeField, Min(0f)] private float _gravity = 24f;
        [SerializeField] private float _groundY = -3f;

        private readonly InputBuffer _jumpBuffer = new(0.15f);
        private InputAction _move;
        private InputAction _jump;
        private float _velocityY;

        public bool IsGrounded => transform.position.y <= _groundY + 0.001f && _velocityY <= 0f;

        public bool JumpBuffered => _jumpBuffer.Peek(Time.time);

        private void Start()
        {
            _move = _input.Actions.FindAction("Gameplay/Move", true);
            _jump = _input.Actions.FindAction("Gameplay/Jump", true);
            _jump.performed += OnJump;
        }

        private void OnDestroy()
        {
            if (_jump != null)
            {
                _jump.performed -= OnJump;
            }
        }

        private void Update()
        {
            Vector3 position = transform.position;
            position.x += _move.ReadValue<Vector2>().x * _speed * Time.deltaTime;

            if (IsGrounded && _jumpBuffer.Consume(Time.time))
            {
                _velocityY = _jumpSpeed;
            }

            _velocityY -= _gravity * Time.deltaTime;
            position.y += _velocityY * Time.deltaTime;
            if (position.y <= _groundY)
            {
                position.y = _groundY;
                _velocityY = Mathf.Min(0f, _velocityY);
            }

            transform.position = position;
        }

        private void OnJump(InputAction.CallbackContext context)
        {
            _jumpBuffer.Press(Time.time);
        }
    }
}
