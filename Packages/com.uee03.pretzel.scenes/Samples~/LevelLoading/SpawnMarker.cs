using UnityEngine;

namespace Pretzel.Samples.LevelLoading
{
    /// <summary>Marks where the level spawns an enemy. Designers place markers; the objects come from pools at load time.</summary>
    public sealed class SpawnMarker : MonoBehaviour
    {
        [SerializeField] private EnemyKind _kind = EnemyKind.Grunt;

        [Tooltip("Enemies spawned in a ring around this marker.")]
        [SerializeField, Min(1)] private int _count = 1;

        [SerializeField, Min(0f)] private float _radius = 1.5f;

        public EnemyKind Kind => _kind;

        public int Count => _count;

        public Vector3 GetPosition(int index)
        {
            if (_count == 1)
            {
                return transform.position;
            }

            float angle = index * Mathf.PI * 2f / _count;
            return transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _radius;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = _kind == EnemyKind.Brute ? Color.red : Color.yellow;
            for (int i = 0; i < _count; i++)
            {
                Gizmos.DrawWireSphere(GetPosition(i), 0.3f);
            }
        }
    }
}
