using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Pretzel.Data.Editor
{
    /// <summary>Finds and creates the <see cref="DataIOConfig"/> asset that <see cref="DataIO"/> reads at runtime.</summary>
    internal static class DataIOConfigAsset
    {
        public const string AssetPath = "Assets/Resources/" + DataIOConfig.ResourcePath + ".asset";

        public static DataIOConfig Load() => AssetDatabase.LoadAssetAtPath<DataIOConfig>(AssetPath);

        public static DataIOConfig LoadOrCreate()
        {
            DataIOConfig config = Load();
            if (config)
            {
                return config;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
            config = ScriptableObject.CreateInstance<DataIOConfig>();
            config.environmentPrefix = DefaultPrefix();
            AssetDatabase.CreateAsset(config, AssetPath);
            AssetDatabase.SaveAssets();
            return config;
        }

        /// <summary>PRETZEL_ plus the product name in capitals, so two projects do not share variables by accident.</summary>
        public static string DefaultPrefix()
        {
            char[] letters = PlayerSettings.productName.ToUpperInvariant().ToCharArray();
            for (int i = 0; i < letters.Length; i++)
            {
                if (!char.IsLetterOrDigit(letters[i]) || letters[i] > 'Z')
                {
                    letters[i] = '_';
                }
            }

            string name = new string(letters).Trim('_');
            if (name.Length == 0)
            {
                return "PRETZEL_GAME";
            }

            return name.StartsWith("PRETZEL", StringComparison.Ordinal) ? name : "PRETZEL_" + name;
        }

        /// <summary>A prefix must make valid variable names: letters, digits and underscores, not starting with a digit.</summary>
        public static bool IsValidPrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix) || char.IsDigit(prefix[0]))
            {
                return false;
            }

            foreach (char c in prefix)
            {
                if (!(c == '_' || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
