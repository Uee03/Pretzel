using UnityEngine;

namespace Pretzel.UI
{
    /// <summary>
    /// Turns this transform toward the camera every frame. Use it on a world-space UIDocument (Unity 6.2 or newer) for name tags
    /// and interaction prompts, or on any other world-space visual. Optionally keeps a constant size on screen.
    /// </summary>
    [AddComponentMenu("Pretzel/UI/UI Billboard")]
    public sealed class UIBillboard : MonoBehaviour
    {
        [SerializeField] private BillboardMode _mode = BillboardMode.MatchCamera;
        [Tooltip("Defaults to Camera.main, looked up once.")]
        [SerializeField] private Camera _camera;

        [Tooltip("Turns the result 180 degrees around its up axis, for content authored to face the other way.")]
        [SerializeField] private bool _flip;

        [Tooltip("Scales with distance so the object keeps its size on screen. Perspective cameras only.")]
        [SerializeField] private bool _constantScreenSize;
        [Tooltip("Distance at which the object has its original scale.")]
        [SerializeField, Min(0.01f)] private float _referenceDistance = 10f;

        private Vector3 _baseScale;

        public BillboardMode Mode
        {
            get => _mode;
            set => _mode = value;
        }

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        private void LateUpdate()
        {
            if (!_camera)
            {
                _camera = Camera.main;
                if (!_camera)
                {
                    return;
                }
            }

            Transform self = transform;
            Transform view = _camera.transform;
            switch (_mode)
            {
                case BillboardMode.MatchCamera:
                    self.rotation = view.rotation;
                    break;
                case BillboardMode.FaceCamera:
                    Vector3 away = self.position - view.position;
                    if (away.sqrMagnitude > 0.0001f)
                    {
                        self.rotation = Quaternion.LookRotation(away, view.up);
                    }

                    break;
                case BillboardMode.UprightFaceCamera:
                    Vector3 flat = self.position - view.position;
                    flat.y = 0f;
                    if (flat.sqrMagnitude > 0.0001f)
                    {
                        self.rotation = Quaternion.LookRotation(flat, Vector3.up);
                    }

                    break;
            }

            if (_flip)
            {
                self.rotation *= Quaternion.Euler(0f, 180f, 0f);
            }

            if (_constantScreenSize && !_camera.orthographic)
            {
                float distance = Vector3.Dot(self.position - view.position, view.forward);
                self.localScale = _baseScale * Mathf.Max(0.01f, distance / _referenceDistance);
            }
        }
    }
}
