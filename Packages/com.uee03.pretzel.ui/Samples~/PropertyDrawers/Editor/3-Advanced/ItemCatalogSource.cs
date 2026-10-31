using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Pretzel.Samples.UI.PropertyDrawers.Editors
{
    /// <summary>
    /// The data source behind the loot dropdown: the ids of every <see cref="ItemCatalog"/> asset in the project, loaded
    /// lazily and reloaded when a catalog is imported. Drawers subscribe to <see cref="Changed"/> while they are on screen.
    /// </summary>
    internal static class ItemCatalogSource
    {
        private static readonly List<string> _ids = new();
        private static readonly HashSet<string> _lookup = new(StringComparer.Ordinal);
        private static bool _loaded;
        private static bool _hasCatalog;

        public static event Action Changed;

        public static bool HasCatalog
        {
            get
            {
                EnsureLoaded();
                return _hasCatalog;
            }
        }

        public static void FillIds(List<string> target)
        {
            EnsureLoaded();
            target.Clear();
            target.AddRange(_ids);
        }

        public static bool Contains(string id)
        {
            EnsureLoaded();
            return id != null && _lookup.Contains(id);
        }

        /// <summary>Reloads only after the first use, so importing assets costs nothing until a drawer asked for the data.</summary>
        public static void ReloadIfLoaded()
        {
            if (_loaded)
            {
                Reload();
            }
        }

        public static void CreateDefaultCatalog()
        {
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/ItemCatalog.asset");
            ItemCatalog catalog = ScriptableObject.CreateInstance<ItemCatalog>();
            AssetDatabase.CreateAsset(catalog, path);
            EditorGUIUtility.PingObject(catalog);
            Reload();
        }

        private static void Reload()
        {
            Load();
            Changed?.Invoke();
        }

        private static void EnsureLoaded()
        {
            if (!_loaded)
            {
                Load();
            }
        }

        private static void Load()
        {
            _ids.Clear();
            _lookup.Clear();
            _hasCatalog = false;
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(ItemCatalog)))
            {
                ItemCatalog catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(AssetDatabase.GUIDToAssetPath(guid));
                if (!catalog)
                {
                    continue;
                }

                _hasCatalog = true;
                foreach (string id in catalog.itemIds)
                {
                    if (!string.IsNullOrEmpty(id) && _lookup.Add(id))
                    {
                        _ids.Add(id);
                    }
                }
            }

            _loaded = true;
        }
    }
}
