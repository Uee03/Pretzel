using UnityEngine;

namespace Pretzel.Samples.UI.CustomInspectors
{
    /// <summary>Target of the Components inspector: the editor spawns instances of <see cref="prefab"/> inside a disc.</summary>
    [AddComponentMenu("Pretzel Samples/Spawn Area")]
    public sealed class SpawnArea : MonoBehaviour
    {
        public const string ContainerName = "Spawned";

        public GameObject prefab;
        [Min(1)] public int count = 8;
        [Min(0.1f)] public float radius = 4f;
        public int seed = 1;

        /// <summary>A random point in the disc on the local XZ plane.</summary>
        public Vector3 SamplePoint(System.Random random)
        {
            float angle = (float)random.NextDouble() * Mathf.PI * 2f;
            float distance = Mathf.Sqrt((float)random.NextDouble()) * radius;
            return new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireSphere(Vector3.zero, radius);
        }
    }
}
