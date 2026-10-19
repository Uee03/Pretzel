using System.IO;
using UnityEditor;
using UnityEngine;

namespace Pretzel.Setup.Paths
{
    public static class PretzelPaths
    {
        /// <summary>The folder that contains Assets.</summary>
        public static string ProjectFolder => Directory.GetParent(Application.dataPath).FullName;

        /// <summary>The OS temp folder for this product. Read on demand, because the product name can change.</summary>
        public static string SystemTempFolder
        {
            get
            {
                string product = PlayerSettings.productName;
                foreach (char invalid in Path.GetInvalidFileNameChars())
                {
                    product = product.Replace(invalid, '_');
                }

                return Path.Combine(Path.GetTempPath(), string.IsNullOrWhiteSpace(product) ? "Unity" : product);
            }
        }

        /// <summary>Opens a folder in the OS file browser. Returns false when it does not exist.</summary>
        public static bool Open(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                return false;
            }

            EditorUtility.RevealInFinder(folder);
            return true;
        }
    }
}
