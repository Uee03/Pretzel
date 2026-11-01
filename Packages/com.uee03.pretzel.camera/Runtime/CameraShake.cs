using Unity.Cinemachine;
using UnityEngine;

namespace Pretzel.Cameras
{
    /// <summary>
    /// Trauma-based screen shake as a Cinemachine extension. Add it to a CinemachineCamera and call
    /// <see cref="AddTrauma"/>. The offset is applied at the end of the camera pipeline, so it never fights the follow,
    /// the confiner or a blend, and it costs three noise samples while shaking and nothing while still.
    /// </summary>
    [AddComponentMenu("Pretzel/Camera/Camera Shake")]
    [DisallowMultipleComponent]
    public sealed class CameraShake : CinemachineExtension, ICameraShaker
    {
        [Tooltip("The largest offset, in the camera's own axes and world units, at full trauma. Leave Z at 0 for a 2D camera.")]
        [SerializeField] private Vector3 _maxOffset = new(0.35f, 0.35f, 0f);

        [Tooltip("The largest roll, in degrees, at full trauma.")]
        [SerializeField, Min(0f)] private float _maxRollDegrees = 2f;

        [Tooltip("Trauma lost per second. 1.2 means a full shake lasts under a second.")]
        [SerializeField, Min(0f)] private float _decayPerSecond = 1.2f;

        [Tooltip("How fast the shake changes, in cycles per second.")]
        [SerializeField, Min(0f)] private float _frequency = 25f;

        [Tooltip("Run on unscaled time, so the shake keeps decaying during hit stop and pauses instead of freezing the camera off-centre.")]
        [SerializeField] private bool _unscaledTime = true;

        private TraumaShake _shake;

        public float Trauma => Shake.Trauma;

        private TraumaShake Shake
        {
            get
            {
                if (_shake == null)
                {
                    _shake = new TraumaShake(_decayPerSecond, _frequency);
                }

                return _shake;
            }
        }

        public void AddTrauma(float amount)
        {
            Shake.Add(amount);
        }

        /// <summary>Adds trauma but not beyond <paramref name="ceiling"/>, for rumbles that must not drown out a big hit.</summary>
        public void AddTraumaUpTo(float amount, float ceiling)
        {
            Shake.AddUpTo(amount, ceiling);
        }

        public void ClearTrauma()
        {
            Shake.Clear();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Shake.Clear();
        }

        private void OnValidate()
        {
            if (_shake != null)
            {
                _shake.DecayPerSecond = _decayPerSecond;
                _shake.Frequency = _frequency;
            }
        }

        private void Update()
        {
            if (_shake != null)
            {
                _shake.Advance(_unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
            }
        }

        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Finalize || _shake == null || !_shake.IsShaking)
            {
                return;
            }

            state.PositionCorrection += state.GetCorrectedOrientation() * _shake.Offset(_maxOffset);
            float roll = _shake.Roll(_maxRollDegrees);
            if (roll != 0f)
            {
                state.OrientationCorrection = state.OrientationCorrection * Quaternion.AngleAxis(roll, Vector3.forward);
            }
        }
    }
}
