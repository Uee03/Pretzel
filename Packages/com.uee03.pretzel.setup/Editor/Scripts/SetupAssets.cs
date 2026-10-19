using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Setup
{
    /// <summary>Loads the assets that ship inside this package.</summary>
    internal static class SetupAssets
    {
        public const string PackageId = "com.uee03.pretzel.setup";
        public const string Root = "Packages/" + PackageId + "/Editor/";

        public static VisualTreeAsset Layout(string name) => Load<VisualTreeAsset>("Layouts/" + name + ".uxml");

        public static StyleSheet Style(string name) => Load<StyleSheet>("Styles/" + name + ".uss");

        public static Texture2D Branding(string name) => Load<Texture2D>("Branding/" + name + ".png");

        public static string PackageVersion()
        {
            UnityEditor.PackageManager.PackageInfo info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(SetupAssets).Assembly);
            return info != null ? info.version : "dev";
        }

        private static T Load<T>(string relativePath) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(Root + relativePath);
            if (!asset)
            {
                Debug.LogWarning($"[Pretzel] Missing editor asset '{Root + relativePath}'.");
            }

            return asset;
        }
    }
}
