using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Data.Editor
{
    /// <summary>Loads the editor assets that ship inside this package.</summary>
    internal static class DataEditorAssets
    {
        private const string Root = "Packages/com.uee03.pretzel.data/Editor/";

        public static VisualTreeAsset Layout(string name) => Load<VisualTreeAsset>("Layouts/" + name + ".uxml");

        public static StyleSheet Style(string name) => Load<StyleSheet>("Styles/" + name + ".uss");

        public static Texture2D Branding(string name) => Load<Texture2D>("Branding/" + name + ".png");

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
