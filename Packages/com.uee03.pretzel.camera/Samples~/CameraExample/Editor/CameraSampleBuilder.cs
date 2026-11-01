using System.IO;
using Pretzel.Cameras.Editor;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Pretzel.Cameras.Samples.Editor
{
    /// <summary>
    /// Imports the sample art and builds the scene with the same rig builder the GameObject menu uses. The sample ships
    /// with the scene built; run it again after changing the art or the scripts.
    /// </summary>
    public static class CameraSampleBuilder
    {
        [MenuItem("Pretzel/Samples/Camera/Build Scene And Assets")]
        public static void Build()
        {
            string root = SampleRoot();
            ImportSprite(root + "/Art/arena.png", 32f);
            ImportSprite(root + "/Art/orb.png", 64f);
            CreatePanelSettings(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            BuildScene(root, root + "/Scenes/CameraExample.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("[Camera] Built the sample scene. Open CameraExample and press Play.");
        }

        private static string SampleRoot()
        {
            foreach (string guid in AssetDatabase.FindAssets("CameraSampleBuilder t:MonoScript"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("/CameraSampleBuilder.cs", System.StringComparison.Ordinal))
                {
                    return Path.GetDirectoryName(Path.GetDirectoryName(path)).Replace('\\', '/');
                }
            }

            throw new FileNotFoundException("Cannot locate the Camera sample folder.");
        }

        private static void ImportSprite(string path, float pixelsPerUnit)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                throw new FileNotFoundException("Missing sample art: " + path);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        private static void CreatePanelSettings(string root)
        {
            string path = root + "/Styles/CameraExamplePanel.asset";
            PanelSettings panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, path);
            }

            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(root + "/Styles/CameraExampleTheme.tss");
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1280, 720);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.5f;
            EditorUtility.SetDirty(panel);
        }

        private static void BuildScene(string root, string path)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Material material = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

            GameObject arena = new("Arena", typeof(SpriteRenderer));
            SpriteRenderer arenaRenderer = arena.GetComponent<SpriteRenderer>();
            arenaRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(root + "/Art/arena.png");
            arenaRenderer.sharedMaterial = material;
            arenaRenderer.sortingOrder = -10;

            GameObject orb = new("Orb", typeof(SpriteRenderer), typeof(SampleWanderer));
            SpriteRenderer orbRenderer = orb.GetComponent<SpriteRenderer>();
            orbRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(root + "/Art/orb.png");
            orbRenderer.sharedMaterial = material;
            orbRenderer.sortingOrder = 10;

            CinemachineCamera rig = CameraRigBuilder.Create(CameraRigKind.TwoD, orb.transform);
            Camera camera = Camera.main;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.1f, 0.15f);

            GameObject ui = new("Panel", typeof(UIDocument));
            ui.GetComponent<UIDocument>().panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(root + "/Styles/CameraExamplePanel.asset");
            CameraDemoPanel panel = ui.AddComponent<CameraDemoPanel>();
            SerializedObject serialized = new(panel);
            serialized.FindProperty("_shake").objectReferenceValue = rig.GetComponent<CameraShake>();
            serialized.FindProperty("_fitter").objectReferenceValue = rig.GetComponent<PlayfieldFitter>();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
