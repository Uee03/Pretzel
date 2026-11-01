using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Pretzel.Inputs.Samples.Editor
{
    /// <summary>Builds the sample's actions asset, sprite, panel and scene. The sample ships with them built; run it again after changing the maps.</summary>
    public static class InputSampleBuilder
    {
        [MenuItem("Pretzel/Samples/Input/Build Scene And Assets")]
        public static void Build()
        {
            string root = SampleRoot();
            Directory.CreateDirectory(root + "/Input");
            Directory.CreateDirectory(root + "/Art");
            Directory.CreateDirectory(root + "/Scenes");
            WriteActions(root + "/Input/ExampleActions.inputactions");
            WriteSquare(root + "/Art/square.png");
            AssetDatabase.Refresh();
            ImportSquare(root + "/Art/square.png");
            CreatePanelSettings(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            BuildScene(root, root + "/Scenes/InputExample.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("[Input] Built the sample scene. Open InputExample and press Play.");
        }

        private static string SampleRoot()
        {
            foreach (string guid in AssetDatabase.FindAssets("InputSampleBuilder t:MonoScript"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("/InputSampleBuilder.cs", System.StringComparison.Ordinal))
                {
                    return Path.GetDirectoryName(Path.GetDirectoryName(path)).Replace('\\', '/');
                }
            }

            throw new FileNotFoundException("Cannot locate the Input sample folder.");
        }

        // One map per context, named after it.
        private static void WriteActions(string path)
        {
            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();

            InputActionMap gameplay = asset.AddActionMap("Gameplay");
            InputAction move = gameplay.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");

            InputAction jump = gameplay.AddAction("Jump", InputActionType.Button);
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            InputAction pause = gameplay.AddAction("Pause", InputActionType.Button);
            pause.AddBinding("<Keyboard>/escape");
            pause.AddBinding("<Gamepad>/start");

            InputActionMap menu = asset.AddActionMap("Menu");
            InputAction resume = menu.AddAction("Resume", InputActionType.Button);
            resume.AddBinding("<Keyboard>/escape");
            resume.AddBinding("<Gamepad>/start");

            File.WriteAllText(path, asset.ToJson());
            Object.DestroyImmediate(asset);
        }

        private static void WriteSquare(string path)
        {
            Texture2D texture = new(32, 32, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            texture.SetPixels(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        private static void ImportSquare(string path)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static void CreatePanelSettings(string root)
        {
            string path = root + "/Styles/InputExamplePanel.asset";
            PanelSettings panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, path);
            }

            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(root + "/Styles/InputExampleTheme.tss");
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
            Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(root + "/Art/square.png");

            GameObject cameraObject = new("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.16f, 0.24f);

            GameObject ground = new("Ground", typeof(SpriteRenderer));
            ground.transform.position = new Vector3(0f, -3.75f, 0f);
            ground.transform.localScale = new Vector3(40f, 0.5f, 1f);
            SpriteRenderer groundRenderer = ground.GetComponent<SpriteRenderer>();
            groundRenderer.sprite = square;
            groundRenderer.sharedMaterial = material;
            groundRenderer.color = new Color(0.25f, 0.33f, 0.45f);

            GameObject inputObject = new("Input", typeof(InputExample));
            SerializedObject inputSerialized = new(inputObject.GetComponent<InputExample>());
            inputSerialized.FindProperty("_actions").objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputActionAsset>(root + "/Input/ExampleActions.inputactions");
            inputSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject player = new("Player", typeof(SpriteRenderer), typeof(ExamplePlayer));
            player.transform.position = new Vector3(0f, -3f, 0f);
            SpriteRenderer playerRenderer = player.GetComponent<SpriteRenderer>();
            playerRenderer.sprite = square;
            playerRenderer.sharedMaterial = material;
            playerRenderer.color = new Color(1f, 0.7f, 0.25f);
            playerRenderer.sortingOrder = 5;
            SerializedObject playerSerialized = new(player.GetComponent<ExamplePlayer>());
            playerSerialized.FindProperty("_input").objectReferenceValue = inputObject.GetComponent<InputExample>();
            playerSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject ui = new("Panel", typeof(UIDocument));
            ui.GetComponent<UIDocument>().panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(root + "/Styles/InputExamplePanel.asset");
            InputExamplePanel panel = ui.AddComponent<InputExamplePanel>();
            SerializedObject panelSerialized = new(panel);
            panelSerialized.FindProperty("_input").objectReferenceValue = inputObject.GetComponent<InputExample>();
            panelSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
