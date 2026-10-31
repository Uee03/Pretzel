using System.Collections.Generic;
using UnityEngine;

namespace Pretzel.Samples.UI.PropertyDrawers
{
    /// <summary>Add this to a GameObject to see the advanced drawers: loot entries in a list and polymorphic effects.</summary>
    [AddComponentMenu("Pretzel Samples/3 Advanced Drawer Demo")]
    public sealed class AdvancedDrawerDemo : MonoBehaviour
    {
        public List<LootEntry> loot = new() { new LootEntry { itemId = "potion" } };

        [SerializeReference, SerializeReferenceSelector]
        public AbilityEffect primaryEffect = new DamageEffect();

        [SerializeReference, SerializeReferenceSelector]
        public List<AbilityEffect> effects = new();
    }
}
