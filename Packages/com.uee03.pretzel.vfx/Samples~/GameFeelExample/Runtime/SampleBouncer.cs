using UnityEngine;

namespace Pretzel.Vfx.Samples
{
    /// <summary>Bounces a sprite around the view on scaled time, so a hit stop or slow motion is easy to see.</summary>
    public sealed class SampleBouncer : MonoBehaviour
    {
        [SerializeField] private Vector2 _bounds = new(7f, 3.5f);
        [SerializeField] private Vector2 _velocity = new(4.5f, 3.2f);

        private void Update()
        {
            Vector3 position = transform.position + (Vector3)(_velocity * Time.deltaTime);
            if (Mathf.Abs(position.x) > _bounds.x)
            {
                _velocity.x = -_velocity.x;
                position.x = Mathf.Clamp(position.x, -_bounds.x, _bounds.x);
            }

            if (Mathf.Abs(position.y) > _bounds.y)
            {
                _velocity.y = -_velocity.y;
                position.y = Mathf.Clamp(position.y, -_bounds.y, _bounds.y);
            }

            transform.position = position;
        }
    }
}
