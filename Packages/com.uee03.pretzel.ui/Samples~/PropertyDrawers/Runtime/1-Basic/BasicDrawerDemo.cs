using UnityEngine;

namespace Pretzel.Samples.UI.PropertyDrawers
{
    /// <summary>Add this to a GameObject to see the basic attribute drawers in the Inspector.</summary>
    [AddComponentMenu("Pretzel Samples/1 Basic Drawer Demo")]
    public sealed class BasicDrawerDemo : MonoBehaviour
    {
        [ReadOnlyField] public string id = "player-001";
        [ReadOnlyField] public int spawnCount = 3;
        public string displayName = "Hero";

        [ProgressBar(0f, 100f)] public float health = 65f;
        [ProgressBar(0f, 1f)] public float loadProgress = 0.4f;
        [ProgressBar(0f, 10f)] public int level = 3;
    }
}
