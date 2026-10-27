using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pretzel.Setup.Addons.Controls;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Pretzel.Setup.Addons.Services
{
    /// <summary>Imports the samples a package declares in its package.json and shows them in the Project window.</summary>
    public static class SampleService
    {
        private const string ReadmeName = "README.md";

        public static IReadOnlyList<Sample> Find(PackageInfo info)
        {
            return info == null ? Array.Empty<Sample>() : Sample.FindByPackage(info.name, info.version).ToList();
        }

        /// <summary>A card action that imports the samples on first use and then selects them.</summary>
        public static CardAction CreateAction(PackageInfo info, bool busy)
        {
            IReadOnlyList<Sample> samples = Find(info);
            if (samples.Count == 0)
            {
                return new CardAction("Samples", null, false, tooltip: "This package has no samples.");
            }

            bool imported = samples.All(s => s.isImported);
            string tooltip = (imported ? "Show" : "Import and show") + " in the Project window:\n"
                + string.Join("\n", samples.Select(s => "- " + s.displayName + ": " + s.description));
            return new CardAction("Samples", () => Open(samples), !busy && !EditorApplication.isCompiling, tooltip: tooltip);
        }

        /// <summary>Imports any sample that is missing, then selects its README, or the samples folder when there are several.</summary>
        public static void Open(IReadOnlyList<Sample> samples)
        {
            foreach (Sample sample in samples)
            {
                if (!sample.isImported && !sample.Import(Sample.ImportOptions.None))
                {
                    Debug.LogWarning($"[Pretzel] Could not import the sample '{sample.displayName}'.");
                    return;
                }
            }

            string folder = ToProjectPath(samples.Count == 1 ? samples[0].importPath : Path.GetDirectoryName(samples[0].importPath));
            if (folder == null)
            {
                Debug.LogWarning("[Pretzel] The samples were imported outside the Assets folder: " + samples[0].importPath);
                return;
            }

            Reveal(folder);
        }

        private static void Reveal(string folder)
        {
            UnityEngine.Object target = AssetDatabase.LoadAssetAtPath<TextAsset>(folder + "/" + ReadmeName);
            if (!target)
            {
                target = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folder);
            }

            if (!target)
            {
                return;
            }

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = target;
            EditorGUIUtility.PingObject(target);
        }

        // Sample.importPath is absolute. AssetDatabase needs a path that starts with Assets.
        private static string ToProjectPath(string absolutePath)
        {
            string path = absolutePath.Replace('\\', '/').TrimEnd('/');
            string dataPath = Application.dataPath.Replace('\\', '/');
            if (path.Equals(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                return "Assets";
            }

            return path.StartsWith(dataPath + "/", StringComparison.OrdinalIgnoreCase)
                ? "Assets" + path.Substring(dataPath.Length)
                : null;
        }
    }
}
