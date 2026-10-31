using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.CustomInspectors.Editors
{
    /// <summary>Finds sample USS by file name, because samples are imported to <c>Assets/Samples/&lt;name&gt;/&lt;version&gt;/</c>.</summary>
    internal static class SampleAssets
    {
        public static StyleSheet FindStyleSheet(string fileName)
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

                StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                if (sheet)
                {
                    return sheet;
                }
            }

            Debug.LogWarning($"Sample stylesheet '{fileName}' was not found. Keep the sample folder inside the Assets folder.");
            return null;
        }

        public static void AddStyleSheet(VisualElement root, string fileName)
        {
            StyleSheet sheet = FindStyleSheet(fileName);
            if (sheet)
            {
                root.styleSheets.Add(sheet);
            }
        }
    }
}
