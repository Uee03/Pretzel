using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Pretzel.Vfx.Samples.Editor
{
    /// <summary>Imports the sample art and builds the scene. The sample ships with it built; run it again after changing the art.</summary>
    public static class GameFeelSampleBuilder
    {
        [MenuItem("Pretzel/Samples/VFX/Build Scene And Assets")]
        public static void Build()
        {
            string root = SampleRoot();
            ImportSprite(root + "/Art/face.png");
            CreatePanelSettings(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            BuildScene(root, root + "/Scenes/GameFeelExample.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("[VFX] Built the sample scene. Open GameFeelExample and press Play.");
        }

        private static string SampleRoot()
        {
            foreach (string guid in AssetDatabase.FindAssets("GameFeelSampleBuilder t:MonoScript"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("/GameFeelSampleBuilder.cs", System.StringComparison.Ordinal))
                {
                    return Path.GetDirectoryName(Path.GetDirectoryName(path)).Replace('\\', '/');
                }
            }

            throw new FileNotFoundException("Cannot locate the Game Feel sample folder.");
        }

        private static void ImportSprite(string path)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                throw new FileNotFoundException("Missing sample art: " + path);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        private static void CreatePanelSettings(string root)
        {
            string path = root + "/Styles/GameFeelPanel.asset";
            PanelSettings panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, path);
            }

            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(root + "/Styles/GameFeelTheme.tss");
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1280, 720);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.5f;
            EditorUtility.SetDirty(panel);
        }

        private static void BuildScene(string root, string path)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraObject = new("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.2f, 0.3f);

            Material material = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            GameObject face = new("Face", typeof(SpriteRenderer), typeof(SampleBouncer));
            SpriteRenderer renderer = face.GetComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(root + "/Art/face.png");
            renderer.sharedMaterial = material;
            SpriteFlash flash = face.AddComponent<SpriteFlash>();
            PunchScale punch = face.AddComponent<PunchScale>();

            GameObject ui = new("Panel", typeof(UIDocument));
            ui.GetComponent<UIDocument>().panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(root + "/Styles/GameFeelPanel.asset");
            FeelPanel panel = ui.AddComponent<FeelPanel>();
            SerializedObject serialized = new(panel);
            serialized.FindProperty("_flash").objectReferenceValue = flash;
            serialized.FindProperty("_punch").objectReferenceValue = punch;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
