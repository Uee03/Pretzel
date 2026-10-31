using UnityEngine;

namespace Pretzel.Samples.UI.CustomInspectors
{
    /// <summary>Target of the FullyCustom inspector: which fields matter depends on <see cref="kind"/>.</summary>
    [AddComponentMenu("Pretzel Samples/Weapon Config")]
    public sealed class WeaponConfig : MonoBehaviour
    {
        public string displayName = "New Weapon";
        public WeaponKind kind;
        [Min(0f)] public float damage = 10f;
        [Min(0.01f)] public float cooldown = 0.5f;

        [Header("Melee")]
        [Min(0f)] public float reach = 1.5f;
        public bool cleave;

        [Header("Ranged")]
        [Min(0f)] public float range = 20f;
        [Min(1)] public int magazineSize = 12;
        public bool usesProjectile;
        [Min(0f)] public float projectileSpeed = 30f;

        [Header("Magic")]
        [Min(0f)] public float manaCost = 5f;
        public Color effectColor = Color.cyan;

        public float DamagePerSecond => cooldown > 0f ? damage / cooldown : 0f;
    }
}
