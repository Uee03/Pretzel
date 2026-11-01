using UnityEngine;

namespace Pretzel.Parallax.Samples
{
    /// <summary>Runs a character to the right with a small hop, so the camera has something to follow.</summary>
    public sealed class SampleRunner : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _speed = 4f;
        [SerializeField, Min(0f)] private float _hopHeight = 0.25f;
        [SerializeField, Min(0.1f)] private float _hopsPerSecond = 2.5f;

        private float _groundY;

        private void Awake()
        {
            _groundY = transform.position.y;
        }

        private void Update()
        {
            Vector3 position = transform.position;
            position.x += _speed * Time.deltaTime;
            position.y = _groundY + Mathf.Abs(Mathf.Sin(Time.time * Mathf.PI * _hopsPerSecond)) * _hopHeight;
            transform.position = position;
        }
    }
}
