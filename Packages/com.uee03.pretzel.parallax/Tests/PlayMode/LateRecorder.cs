using UnityEngine;

namespace Pretzel.Parallax.PlayTests
{
    /// <summary>Samples a camera and a layer at the end of LateUpdate, which is what the renderer sees.</summary>
    [DefaultExecutionOrder(1000)]
    internal sealed class LateRecorder : MonoBehaviour
    {
        public Transform Camera;
        public Transform Layer;
        public float CameraX;
        public float LayerX;
        public int Samples;

        private void LateUpdate()
        {
            CameraX = Camera.position.x;
            LayerX = Layer.position.x;
            Samples++;
        }
    }
}
