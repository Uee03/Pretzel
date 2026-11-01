using UnityEngine;

namespace Pretzel.Parallax.Samples
{
    /// <summary>
    /// Follows a target on the horizontal axis with a smoothed look-ahead. It runs before the ParallaxController
    /// (order 300), so the layers react to the camera's final position. With Cinemachine, the Brain does this job.
    /// </summary>
    public sealed class SampleCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _lookAhead = 3f;
        [SerializeField, Min(0.01f)] private float _smoothTime = 0.25f;

        private float _velocity;

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            Vector3 position = transform.position;
            position.x = Mathf.SmoothDamp(position.x, _target.position.x + _lookAhead, ref _velocity, _smoothTime);
            transform.position = position;
        }
    }
}
