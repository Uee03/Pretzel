using Unity.Cinemachine;
using UnityEngine;

namespace Pretzel.Cameras
{
    /// <summary>
    /// Keeps a playfield of a given size visible on any aspect ratio. It sets the lens before the camera pipeline runs,
    /// so a confiner or composer works with the final size. It does arithmetic only: no allocations and no searches.
    /// Physical cameras are left alone.
    /// </summary>
    [AddComponentMenu("Pretzel/Camera/Playfield Fitter")]
    [DisallowMultipleComponent]
    public sealed class PlayfieldFitter : CinemachineExtension
    {
        [Tooltip("The area that must stay visible, in world units.")]
        [SerializeField] private Vector2 _playfield = new(16f, 9f);

        [SerializeField] private FitMode _mode = FitMode.Contain;

        [Tooltip("World units added around the playfield on every side.")]
        [SerializeField, Min(0f)] private float _padding;

        [Tooltip("Perspective cameras only: the distance from the camera to the playfield's plane.")]
        [SerializeField, Min(0.01f)] private float _distance = 10f;

        public Vector2 Playfield => _playfield;

        public FitMode Mode => _mode;

        public void Configure(Vector2 playfield, FitMode mode = FitMode.Contain, float padding = 0f, float distance = 10f)
        {
            _playfield = playfield;
            _mode = mode;
            _padding = Mathf.Max(0f, padding);
            _distance = Mathf.Max(0.01f, distance);
        }

        public override void PrePipelineMutateCameraStateCallback(CinemachineVirtualCameraBase vcam, ref CameraState curState, float deltaTime)
        {
            if (curState.Lens.IsPhysicalCamera)
            {
                return;
            }

            Vector2 size = _playfield + new Vector2(_padding * 2f, _padding * 2f);
            float aspect = curState.Lens.Aspect;
            if (curState.Lens.Orthographic)
            {
                curState.Lens.OrthographicSize = PlayfieldFit.OrthographicSize(size, aspect, _mode);
            }
            else
            {
                curState.Lens.FieldOfView = PlayfieldFit.VerticalFieldOfView(size, _distance, aspect, _mode);
            }
        }
    }
}
