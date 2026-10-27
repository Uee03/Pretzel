using UnityEngine;

namespace Pretzel.Samples.Templates
{
    /// <summary>Made from Assets > Create > Pretzel > C# Script. Spins this object around an axis.</summary>
    public class Spinner : MonoBehaviour
    {
        #region FIELDS

        [SerializeField] private Vector3 _axis = Vector3.up;
        [SerializeField] private float _degreesPerSecond = 90f;

        private Transform _transform;

        #endregion

        #region UNITY METHODS

        private void Awake()
        {
            _transform = transform;
            _axis = _axis.sqrMagnitude > 0f ? _axis.normalized : Vector3.up;
        }

        private void Update()
        {
            Spin(Time.deltaTime);
        }

        #endregion

        #region METHODS

        private void Spin(float deltaTime)
        {
            _transform.Rotate(_axis, _degreesPerSecond * deltaTime, Space.Self);
        }

        #endregion
    }
}
