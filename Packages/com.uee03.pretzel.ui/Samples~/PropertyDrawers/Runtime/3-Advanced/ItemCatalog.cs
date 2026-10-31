using System.Collections.Generic;
using UnityEngine;

namespace Pretzel.Samples.UI.PropertyDrawers
{
    /// <summary>The data source for the loot entry dropdown: a list of item ids stored in an asset.</summary>
    [CreateAssetMenu(fileName = "ItemCatalog", menuName = "Pretzel Samples/Item Catalog")]
    public sealed class ItemCatalog : ScriptableObject
    {
        public List<string> itemIds = new() { "sword", "shield", "potion", "gold" };
    }
}
