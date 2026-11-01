using UnityEngine;

namespace Pretzel.Cameras.Samples
{
    /// <summary>Moves along a smooth looping path inside the arena, so the camera has something to follow.</summary>
    public sealed class SampleWanderer : MonoBehaviour
    {
        [SerializeField] private Vector2 _extent = new(12f, 6f);
        [SerializeField, Min(0f)] private float _speed = 0.35f;

        private Vector3 _centre;

        private void Awake()
        {
            _centre = transform.position;
        }

        private void Update()
        {
            float t = Time.time * _speed;
            transform.position = _centre + new Vector3(Mathf.Sin(t * 1.3f) * _extent.x, Mathf.Sin(t * 2.1f + 0.7f) * _extent.y, 0f);
        }
    }
}
