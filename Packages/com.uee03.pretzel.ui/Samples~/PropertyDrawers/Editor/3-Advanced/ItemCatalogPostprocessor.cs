using System;
using UnityEditor;

namespace Pretzel.Samples.UI.PropertyDrawers.Editors
{
    /// <summary>Keeps the loot dropdown current when an Item Catalog asset is created, edited and saved, moved or deleted.</summary>
    internal sealed class ItemCatalogPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (AnyAsset(importedAssets) || AnyAsset(deletedAssets) || AnyAsset(movedAssets))
            {
                ItemCatalogSource.ReloadIfLoaded();
            }
        }

        private static bool AnyAsset(string[] paths)
        {
            foreach (string path in paths)
            {
                if (path.EndsWith(".asset", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
