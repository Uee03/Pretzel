using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Pretzel.Samples.NoContainer.Editor
{
    /// <summary>
    /// Creates the sample's services asset, panel settings and two scenes, and registers the scenes for builds.
    /// The sample ships with these already built; run it again to rebuild them after changing the scripts.
    /// </summary>
    public static class NoContainerSampleBuilder
    {
        private const string MenuRoot = "Pretzel/Samples/No Container/";

        [MenuItem(MenuRoot + "Build Scenes And Assets")]
        public static void Build()
        {
            string root = SampleRoot();
            LoadOrCreate<GameServices>(root + "/Resources/GameServices.asset");
            CreatePanel(root);

            // Scenes can only reference assets that are already saved and imported.
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            BuildScene(root, root + "/Scenes/" + SceneNames.Menu + ".unity", isMenu: true);
            BuildScene(root, root + "/Scenes/" + SceneNames.Game + ".unity", isMenu: false);
            AddScenesToBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[NoContainer] Built the sample scenes. Open " + SceneNames.Menu + " and press Play.");
        }

        [MenuItem(MenuRoot + "Add Scenes To Build Settings")]
        public static void AddScenesToBuildSettings()
        {
            string root = SampleRoot();
            string[] paths =
            {
                root + "/Scenes/" + SceneNames.Menu + ".unity",
                root + "/Scenes/" + SceneNames.Game + ".unity"
            };

            System.Collections.Generic.List<EditorBuildSettingsScene> scenes = new(EditorBuildSettings.scenes);
            foreach (string path in paths)
            {
                if (File.Exists(path) && !scenes.Exists(scene => string.Equals(scene.path, path, System.StringComparison.Ordinal)))
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        [MenuItem(MenuRoot + "Delete Saved Progress")]
        public static void DeleteSavedProgress()
        {
            bool deleted = Pretzel.Data.DataIO.Default.Delete(ProgressService.SaveKey);
            Debug.Log(deleted ? "[NoContainer] Deleted the saved progress." : "[NoContainer] There was no saved progress.");
        }

        private static string SampleRoot()
        {
            string[] guids = AssetDatabase.FindAssets("NoContainerSampleBuilder t:MonoScript");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("/NoContainerSampleBuilder.cs", System.StringComparison.Ordinal))
                {
                    return Path.GetDirectoryName(Path.GetDirectoryName(path)).Replace('\\', '/');
                }
            }

            throw new FileNotFoundException("Cannot locate the No Container sample folder.");
        }

        private static PanelSettings CreatePanel(string root)
        {
            PanelSettings panel = LoadOrCreate<PanelSettings>(root + "/UI/NoContainerPanel.asset");
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(root + "/Styles/NoContainerTheme.tss");
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1280, 720);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.5f;
            EditorUtility.SetDirty(panel);
            return panel;
        }

        private static void BuildScene(string root, string path, bool isMenu)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Loaded after NewScene, which unloads unused assets and would invalidate references held from before.
            GameServices services = AssetDatabase.LoadAssetAtPath<GameServices>(root + "/Resources/GameServices.asset");
            PanelSettings panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(root + "/UI/NoContainerPanel.asset");

            GameObject cameraObject = new("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.11f, 0.15f);

            GameObject ui = new(isMenu ? "Menu" : "Game", typeof(UIDocument));
            ui.GetComponent<UIDocument>().panelSettings = panel;

            if (isMenu)
            {
                MenuScreen menu = ui.AddComponent<MenuScreen>();
                SetReference(menu, "_services", services);
            }
            else
            {
                GameController controller = ui.AddComponent<GameController>();
                SetReference(controller, "_services", services);
                GameScreen screen = ui.AddComponent<GameScreen>();
                SetReference(screen, "_services", services);
                SetReference(screen, "_controller", controller);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void SetReference(Object target, string field, Object value)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
