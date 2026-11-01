using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Pretzel.Parallax.Samples.Editor
{
    /// <summary>
    /// Imports the sample art as sprites and builds the two scenes. The sample ships with these built; run it again
    /// after changing the art or the layer table.
    /// </summary>
    public static class ParallaxSampleBuilder
    {
        private const float PixelsPerUnit = 64f;
        private const float CameraDistance = 10f;

        private static readonly System.Collections.Generic.HashSet<string> _bottomPivot = new(System.StringComparer.Ordinal) { "mountains", "hills", "trees" };

        // Name, texture, design y and scale in a 2D view, tiles across, sorting order, 2D factor, depth for 2.5D, and tint.
        private static readonly LayerSpec[] _layers =
        {
            new("Sky", "sky", 0f, new Vector2(1f, 1.4f), new Vector2(5f, 1f), -50, new Vector2(0.95f, 1f), 200f, Color.white),
            new("Mountains", "mountains", -3.2f, Vector2.one, new Vector2(5f, 1f), -40, new Vector2(0.8f, 0.8f), 60f, Color.white),
            new("Hills", "hills", -3.2f, Vector2.one, new Vector2(5f, 1f), -30, new Vector2(0.55f, 0.55f), 30f, Color.white),
            new("Trees", "trees", -3.1f, Vector2.one, new Vector2(5f, 1f), -20, new Vector2(0.3f, 0.3f), 12f, Color.white),
            new("Ground", "ground", -4.5f, Vector2.one, new Vector2(12f, 1f), 0, Vector2.zero, 0f, Color.white),
            new("Foreground Trees", "trees", -6.2f, new Vector2(1.6f, 1.6f), new Vector2(5f, 1f), 20, new Vector2(-0.6f, -0.6f), -5f, new Color(0.6f, 0.8f, 0.7f, 1f))
        };

        [MenuItem("Pretzel/Samples/Parallax/Build Scenes And Assets")]
        public static void Build()
        {
            string root = SampleRoot();
            ImportSprites(root);
            AssetDatabase.Refresh();
            BuildScene(root, root + "/Scenes/Parallax2D.unity", perspective: false);
            BuildScene(root, root + "/Scenes/Parallax25D.unity", perspective: true);
            AssetDatabase.SaveAssets();
            Debug.Log("[Parallax] Built the sample scenes. Open Parallax2D or Parallax25D and press Play.");
        }

        private static string SampleRoot()
        {
            foreach (string guid in AssetDatabase.FindAssets("ParallaxSampleBuilder t:MonoScript"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("/ParallaxSampleBuilder.cs", System.StringComparison.Ordinal))
                {
                    return Path.GetDirectoryName(Path.GetDirectoryName(path)).Replace('\\', '/');
                }
            }

            throw new FileNotFoundException("Cannot locate the Parallax sample folder.");
        }

        private static void ImportSprites(string root)
        {
            foreach (string name in new[] { "sky", "mountains", "hills", "trees", "ground", "runner" })
            {
                string path = root + "/Art/" + name + ".png";
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer == null)
                {
                    throw new FileNotFoundException("Missing sample art: " + path);
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PixelsPerUnit;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;

                // Tiled sprites need a full rectangle mesh, and the silhouettes sit on their bottom edge.
                TextureImporterSettings settings = new();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                bool bottom = _bottomPivot.Contains(name);
                settings.spriteAlignment = (int)(bottom ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        private static void BuildScene(string root, string path, bool perspective)
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Material material = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

            GameObject runner = CreateRunner(root, material);
            Camera camera = CreateCamera(perspective, runner.transform);

            GameObject parallax = new(perspective ? "Parallax (Depth)" : "Parallax");
            ParallaxController controller = parallax.AddComponent<ParallaxController>();
            controller.SetCamera(camera.transform);
            controller.SetFocalDistance(CameraDistance);

            foreach (LayerSpec spec in _layers)
            {
                CreateLayer(root, parallax.transform, spec, perspective, material);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            EditorSceneManager.SaveScene(scene, path);
        }

        private static Camera CreateCamera(bool perspective, Transform target)
        {
            GameObject cameraObject = new("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            cameraObject.transform.position = new Vector3(0f, 0f, -CameraDistance);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.17f, 0.25f, 0.48f);
            camera.orthographic = !perspective;
            camera.orthographicSize = 5f;

            // A field of view that shows the same height at the player's distance as the orthographic camera does.
            camera.fieldOfView = 2f * Mathf.Atan(5f / CameraDistance) * Mathf.Rad2Deg;

            SampleCameraFollow follow = cameraObject.AddComponent<SampleCameraFollow>();
            SerializedObject serialized = new(follow);
            serialized.FindProperty("_target").objectReferenceValue = target;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return camera;
        }

        private static GameObject CreateRunner(string root, Material material)
        {
            GameObject runner = new("Runner", typeof(SpriteRenderer));
            runner.transform.position = new Vector3(0f, -2.5f, 0f);
            SpriteRenderer renderer = runner.GetComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(root + "/Art/runner.png");
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 10;
            runner.AddComponent<SampleRunner>();
            return runner;
        }

        private static void CreateLayer(string root, Transform parent, LayerSpec spec, bool perspective, Material material)
        {
            GameObject layerObject = new(spec.Name, typeof(SpriteRenderer));
            layerObject.transform.SetParent(parent, false);

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(root + "/Art/" + spec.Texture + ".png");
            SpriteRenderer renderer = layerObject.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = new Vector2(sprite.bounds.size.x * spec.Tiles.x, sprite.bounds.size.y * spec.Tiles.y);
            renderer.sortingOrder = spec.Sorting;
            renderer.color = spec.Tint;

            // Under perspective a layer at distance d looks d / CameraDistance times smaller, so it is built that much bigger.
            float scale = perspective ? (spec.Z + CameraDistance) / CameraDistance : 1f;
            layerObject.transform.position = new Vector3(0f, spec.Y * scale, perspective ? spec.Z : 0f);
            layerObject.transform.localScale = new Vector3(spec.Scale.x * scale, spec.Scale.y * scale, 1f);

            ParallaxLayer layer = layerObject.AddComponent<ParallaxLayer>();
            if (perspective)
            {
                layer.ConfigureDepth(Vector2.one, repeatX: true);
            }
            else
            {
                layer.Configure(spec.Factor, repeatX: true);
            }
        }

        private sealed class LayerSpec
        {
            public LayerSpec(string name, string texture, float y, Vector2 scale, Vector2 tiles, int sorting, Vector2 factor, float z, Color tint)
            {
                Name = name;
                Texture = texture;
                Y = y;
                Scale = scale;
                Tiles = tiles;
                Sorting = sorting;
                Factor = factor;
                Z = z;
                Tint = tint;
            }

            public string Name { get; }

            public string Texture { get; }

            public float Y { get; }

            public Vector2 Scale { get; }

            public Vector2 Tiles { get; }

            public int Sorting { get; }

            public Vector2 Factor { get; }

            public float Z { get; }

            public Color Tint { get; }
        }
    }
}
