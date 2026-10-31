using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.PropertyDrawers.Editors
{
    /// <summary>Finds sample USS by file name, because samples are imported to <c>Assets/Samples/&lt;name&gt;/&lt;version&gt;/</c>.</summary>
    internal static class SampleAssets
    {
        public static void AddStyleSheet(VisualElement root, string fileName)
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
                    root.styleSheets.Add(sheet);
                    return;
                }
            }

            Debug.LogWarning($"Sample stylesheet '{fileName}' was not found. Keep the sample folder inside the Assets folder.");
        }
    }
}
