using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.EditorWindows
{
    /// <summary>
    /// Finds sample UXML and USS by file name. Samples are imported to <c>Assets/Samples/&lt;name&gt;/&lt;version&gt;/</c>,
    /// so a fixed asset path would break.
    /// </summary>
    internal static class SampleAssets
    {
        public static T Find<T>(string fileName) where T : UnityEngine.Object
        {
            string search = Path.GetFileNameWithoutExtension(fileName);
            string suffix = "/" + fileName;
            foreach (string guid in AssetDatabase.FindAssets(search))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(suffix, StringComparison.Ordinal))
                {
                    continue;
                }

                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset)
                {
                    return asset;
                }
            }

            Debug.LogWarning($"Sample asset '{fileName}' was not found. Keep the sample folder inside the Assets folder.");
            return null;
        }

        /// <summary>Clones a UXML file into <paramref name="root"/>. Returns false when the file was not found.</summary>
        public static bool CloneLayout(VisualElement root, string fileName)
        {
            VisualTreeAsset tree = Find<VisualTreeAsset>(fileName);
            if (!tree)
            {
                return false;
            }

            tree.CloneTree(root);
            return true;
        }

        public static void AddStyleSheets(VisualElement root, params string[] fileNames)
        {
            foreach (string fileName in fileNames)
            {
                StyleSheet sheet = Find<StyleSheet>(fileName);
                if (sheet)
                {
                    root.styleSheets.Add(sheet);
                }
            }
        }
    }
}
