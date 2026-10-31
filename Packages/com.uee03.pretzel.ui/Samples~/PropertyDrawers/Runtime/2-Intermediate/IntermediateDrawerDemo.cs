using UnityEngine;

namespace Pretzel.Samples.UI.PropertyDrawers
{
    /// <summary>Add this to a GameObject to see the type drawers, the min/max slider and [ShowIf].</summary>
    [AddComponentMenu("Pretzel Samples/2 Intermediate Drawer Demo")]
    public sealed class IntermediateDrawerDemo : MonoBehaviour
    {
        public SpawnSettings spawn = new();
        [MinMaxLimits(0f, 100f)] public FloatRange damage = new(10f, 30f);
        [MinMaxLimits(0f, 5f)] public FloatRange cooldown = new(0.5f, 1.5f);

        public bool useShield;
        [ShowIf(nameof(useShield))] public float shieldStrength = 25f;

        public bool isBoss;
        [ShowIf(nameof(isBoss), true)] public string minionTag = "Minion";
    }
}
