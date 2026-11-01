using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpaceInvaders.Editor
{
    /// <summary>Creates the three PanelSettings assets: HUD and title (sort 0), loading screen (100) and dev console (200).</summary>
    internal static class PanelAssetsBuilder
    {
        public const string Folder = "Assets/_Project/UI/Panels";
        public const string ThemePath = "Assets/_Project/UI/Styles/SpaceInvadersTheme.tss";
        public const string HudPath = Folder + "/HudPanel.asset";
        public const string LoadingPath = Folder + "/LoadingPanel.asset";
        public const string ConsolePath = Folder + "/ConsolePanel.asset";

        public static void BuildAll()
        {
            Build(HudPath, 0);
            Build(LoadingPath, 100);
            Build(ConsolePath, 200);
            AssetDatabase.SaveAssets();
        }

        private static void Build(string path, int sortingOrder)
        {
            BuilderUtil.EnsureFolder(Folder);
            PanelSettings panel = BuilderUtil.LoadOrCreate<PanelSettings>(path);
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1280, 720);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.5f;
            panel.sortingOrder = sortingOrder;
            EditorUtility.SetDirty(panel);
        }
    }
}
