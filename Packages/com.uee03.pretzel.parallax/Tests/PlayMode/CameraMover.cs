using UnityEngine;

namespace Pretzel.Parallax.PlayTests
{
    /// <summary>Moves the camera in Update, the way a follow script or a physics-driven target would.</summary>
    internal sealed class CameraMover : MonoBehaviour
    {
        public float Step = 1f;

        private void Update()
        {
            transform.position += new Vector3(Step, 0f, 0f);
        }
    }
}
