using UnityEngine;

namespace Pretzel.Samples.UI.CustomInspectors
{
    /// <summary>Target of the DefaultPlusExtras inspector: the default fields stay, the editor adds a summary and buttons.</summary>
    [AddComponentMenu("Pretzel Samples/Health Stats")]
    public sealed class HealthStats : MonoBehaviour
    {
        [Min(1)] public int maxHealth = 100;
        [Min(0)] public int health = 100;
        [Tooltip("Health restored per second. Negative values drain health.")]
        public float regenPerSecond = 2f;
        public bool invulnerable;
        [TextArea] public string notes;

        public float HealthFraction => maxHealth > 0 ? Mathf.Clamp01((float)health / maxHealth) : 0f;
    }
}
