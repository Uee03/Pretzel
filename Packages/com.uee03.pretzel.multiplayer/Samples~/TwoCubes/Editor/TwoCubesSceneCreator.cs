using System.Collections.Generic;
using System.IO;
using Pretzel.Multiplayer.Editor;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Pretzel.Multiplayer.Samples.TwoCubes.Editor
{
    /// <summary>
    /// Builds the Two Cubes sample under Assets/PretzelMultiplayerSample: player and item prefabs, a scene with ground, camera, light, NetworkManager, the menu
    /// and the spawn/delete bar, and registers the scene in the build settings. Everything uses supported Editor APIs; nothing is hand-written YAML.
    /// </summary>
    public static class TwoCubesSceneCreator
    {
        public const string Root = "Assets/PretzelMultiplayerSample";

        [MenuItem("Pretzel/Samples/Multiplayer/Create Two Cubes Scene", false, 100)]
        private static void CreateFromMenu3D()
        {
            RunFromMenu(TwoCubesPlane.Horizontal3D);
        }

        [MenuItem("Pretzel/Samples/Multiplayer/Create Two Cubes Scene (2D)", false, 101)]
        private static void CreateFromMenu2D()
        {
            RunFromMenu(TwoCubesPlane.Side2D);
        }

        /// <summary>Entry point for <c>-executeMethod</c> in batch mode.</summary>
        public static void CreateScene3DBatch()
        {
            Debug.Log("[TwoCubes] Created " + Create(TwoCubesPlane.Horizontal3D));
        }

        /// <summary>Entry point for <c>-executeMethod</c> in batch mode.</summary>
        public static void CreateScene2DBatch()
        {
            Debug.Log("[TwoCubes] Created " + Create(TwoCubesPlane.Side2D));
        }

        /// <summary>Creates the assets and the scene and returns the scene asset path.</summary>
        public static string Create(TwoCubesPlane plane)
        {
            AssetFolderUtility.EnsureFolder(Root + "/Prefabs");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            bool is2D = plane == TwoCubesPlane.Side2D;
            Sprite square = is2D ? GetOrCreateSquareSprite() : null;
            string suffix = is2D ? "2D" : "3D";

            GameObject playerPrefab = NetworkPrefabAssetUtility.SavePrefab(BuildPlayer(plane, square), Root + "/Prefabs/TwoCubesPlayer" + suffix + ".prefab");
            GameObject itemPrefab = NetworkPrefabAssetUtility.SavePrefab(BuildItem(plane, square), Root + "/Prefabs/TwoCubesItem" + suffix + ".prefab");

            BuildCamera(is2D);
            if (!is2D)
            {
                BuildLight();
            }

            BuildGround(is2D, square);
            GameObject networkManagerObject = MultiplayerSceneBuilder.CreateNetworkManager(null, Root);
            NetworkManager networkManager = networkManagerObject.GetComponent<NetworkManager>();
            NetworkPrefabListUtility.UseDedicatedList(networkManager, Root, "TwoCubesNetworkPrefabs.asset");
            NetworkPrefabListUtility.Register(networkManager, playerPrefab, Root);
            NetworkPrefabListUtility.Register(networkManager, itemPrefab, Root);

            PretzelNetworkBootstrap bootstrap = networkManagerObject.GetComponent<PretzelNetworkBootstrap>();
            NetworkBootstrapEditorUtility.SetPlayerPrefab(bootstrap, playerPrefab);
            NetworkBootstrapEditorUtility.AddPooledPrefab(bootstrap, itemPrefab, 8, 64);
            NetworkBootstrapEditorUtility.SetSpawnPoints(bootstrap, BuildSpawnPoints());

            TwoCubesItemSpawner spawner = BuildSpawner(itemPrefab);
            MultiplayerSceneBuilder.CreateMenu(null, Root);
            BuildHud(spawner);

            string scenePath = Root + "/TwoCubes" + suffix + ".unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AddToBuildSettings(scenePath);
            RefreshInScenePlacedHashes(scenePath);
            AssetDatabase.SaveAssets();
            return scenePath;
        }

        // NGO writes the GlobalObjectIdHash of a scene-placed NetworkObject in OnValidate, which needs a saved scene: reload it once and save again so the hash is stored.
        private static void RefreshInScenePlacedHashes(string scenePath)
        {
            Scene reloaded = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            EditorSceneManager.MarkSceneDirty(reloaded);
            EditorSceneManager.SaveScene(reloaded);
        }

        private static void RunFromMenu(TwoCubesPlane plane)
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            string path = Create(plane);
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(path));
            Debug.Log("[TwoCubes] Created " + path + " and added it to the build settings. Press Play.");
        }

        private static GameObject BuildPlayer(TwoCubesPlane plane, Sprite square)
        {
            GameObject player;
            if (plane == TwoCubesPlane.Side2D)
            {
                player = new GameObject("TwoCubesPlayer", typeof(SpriteRenderer));
                player.GetComponent<SpriteRenderer>().sprite = square;
                GameObject nose = new GameObject("Nose", typeof(SpriteRenderer));
                nose.transform.SetParent(player.transform, false);
                nose.transform.localPosition = new Vector3(0.55f, 0.1f, 0f);
                nose.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
                SpriteRenderer noseRenderer = nose.GetComponent<SpriteRenderer>();
                noseRenderer.sprite = square;
                noseRenderer.color = new Color(0.1f, 0.1f, 0.1f);
                noseRenderer.sortingOrder = 1;
            }
            else
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Cube);
                player.name = "TwoCubesPlayer";
                Object.DestroyImmediate(player.GetComponent<Collider>());
                GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
                nose.name = "Nose";
                Object.DestroyImmediate(nose.GetComponent<Collider>());
                nose.transform.SetParent(player.transform, false);
                nose.transform.localPosition = new Vector3(0f, 0.1f, 0.55f);
                nose.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            }

            player.AddComponent<NetworkObject>();
            player.AddComponent<NetworkPlayerIdentity>();
            ConfigureTransform(player.AddComponent<NetworkTransform>(), plane == TwoCubesPlane.Side2D);
            TwoCubesPlayer controller = player.AddComponent<TwoCubesPlayer>();
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("_plane").enumValueIndex = (int)plane;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return player;
        }

        // Owner authority: the controlling client moves the cube and NetworkTransform replicates it. Thresholds and synced axes are chosen to send as little as possible.
        private static void ConfigureTransform(NetworkTransform networkTransform, bool is2D)
        {
            networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            networkTransform.Interpolate = true;
            networkTransform.PositionThreshold = 0.01f;
            networkTransform.RotAngleThreshold = 1f;
            networkTransform.SyncRotAngleX = false;
            networkTransform.SyncRotAngleZ = false;
            networkTransform.SyncScaleX = false;
            networkTransform.SyncScaleY = false;
            networkTransform.SyncScaleZ = false;
            networkTransform.SyncPositionZ = !is2D;
        }

        private static GameObject BuildItem(TwoCubesPlane plane, Sprite square)
        {
            GameObject item;
            if (plane == TwoCubesPlane.Side2D)
            {
                item = new GameObject("TwoCubesItem", typeof(SpriteRenderer));
                item.GetComponent<SpriteRenderer>().sprite = square;
                item.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
            }
            else
            {
                item = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                item.name = "TwoCubesItem";
                Object.DestroyImmediate(item.GetComponent<Collider>());
                item.transform.localScale = Vector3.one * 0.45f;
            }

            item.AddComponent<NetworkObject>();
            item.AddComponent<TwoCubesItem>();
            return item;
        }

        private static void BuildCamera(bool is2D)
        {
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.14f, 0.2f);
            if (is2D)
            {
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                cameraObject.transform.position = new Vector3(0f, 2f, -10f);
            }
            else
            {
                cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 13f, -10f), Quaternion.Euler(55f, 0f, 0f));
            }
        }

        private static void BuildLight()
        {
            GameObject lightObject = new GameObject("Directional Light", typeof(Light));
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void BuildGround(bool is2D, Sprite square)
        {
            GameObject ground;
            if (is2D)
            {
                ground = new GameObject("Ground", typeof(SpriteRenderer));
                SpriteRenderer renderer = ground.GetComponent<SpriteRenderer>();
                renderer.sprite = square;
                renderer.color = new Color(0.35f, 0.4f, 0.5f);
                ground.transform.position = new Vector3(0f, -0.2f, 0f);
                ground.transform.localScale = new Vector3(20f, 0.4f, 1f);
                return;
            }

            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            Object.DestroyImmediate(ground.GetComponent<Collider>());
            ground.transform.position = new Vector3(0f, -0.1f, 0f);
            ground.transform.localScale = new Vector3(20f, 0.2f, 20f);
        }

        private static Transform[] BuildSpawnPoints()
        {
            GameObject parent = new GameObject("Spawn Points");
            Vector3[] positions =
            {
                new Vector3(-3f, 0.5f, 0f),
                new Vector3(3f, 0.5f, 0f),
                new Vector3(-3f, 0.5f, 3f),
                new Vector3(3f, 0.5f, 3f),
            };
            Transform[] points = new Transform[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject point = new GameObject("Spawn " + (i + 1));
                point.transform.SetParent(parent.transform, false);
                point.transform.position = positions[i];
                points[i] = point.transform;
            }

            return points;
        }

        private static TwoCubesItemSpawner BuildSpawner(GameObject itemPrefab)
        {
            GameObject go = new GameObject("Item Spawner", typeof(NetworkObject), typeof(TwoCubesItemSpawner));
            TwoCubesItemSpawner spawner = go.GetComponent<TwoCubesItemSpawner>();
            SerializedObject serialized = new SerializedObject(spawner);
            serialized.FindProperty("_itemPrefab").objectReferenceValue = itemPrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return spawner;
        }

        private static void BuildHud(TwoCubesItemSpawner spawner)
        {
            string[] guids = AssetDatabase.FindAssets("TwoCubesHud t:VisualTreeAsset");
            if (guids.Length == 0)
            {
                throw new System.InvalidOperationException("TwoCubesHud.uxml was not found. Import the Two Cubes sample first.");
            }

            VisualTreeAsset layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
            GameObject go = new GameObject("Two Cubes HUD", typeof(UIDocument), typeof(TwoCubesHud));
            UIDocument document = go.GetComponent<UIDocument>();
            document.panelSettings = MultiplayerSceneBuilder.GetOrCreatePanelSettings(Root);
            document.visualTreeAsset = layout;
            SerializedObject serialized = new SerializedObject(go.GetComponent<TwoCubesHud>());
            serialized.FindProperty("_spawner").objectReferenceValue = spawner;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite GetOrCreateSquareSprite()
        {
            string path = Root + "/Sprites/Square.png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
            {
                return existing;
            }

            AssetFolderUtility.EnsureFolder(Root + "/Sprites");
            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[16 * 16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(Application.dataPath), path), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void AddToBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (EditorBuildSettingsScene existing in scenes)
            {
                if (string.Equals(existing.path, scenePath, System.StringComparison.Ordinal))
                {
                    existing.enabled = true;
                    EditorBuildSettings.scenes = scenes.ToArray();
                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
