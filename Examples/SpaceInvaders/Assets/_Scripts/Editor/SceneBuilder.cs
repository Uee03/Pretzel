using System.Collections.Generic;
using Pretzel.Cameras;
using Pretzel.Cameras.Editor;
using Pretzel.DevConsole;
using Pretzel.Integrations.VContainer;
using Pretzel.Parallax;
using SpaceInvaders.Audio;
using SpaceInvaders.Gameplay;
using SpaceInvaders.UI;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.SceneTemplate;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SpaceInvaders.Editor
{
    /// <summary>Builds the Title and Game scenes from the Pretzel 2D scene template, so both start with the standard Pretzel groups.</summary>
    internal static partial class SceneBuilder
    {
        public const string TitleScenePath = "Assets/_Project/Scenes/Title.unity";
        public const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        private const string TemplatePath = "Packages/com.uee03.pretzel.setup/SceneTemplates/Pretzel2D.scenetemplate";
        private const string HudLayoutPath = "Assets/_Project/UI/Layouts/Hud.uxml";
        private const string TitleLayoutPath = "Assets/_Project/UI/Layouts/Title.uxml";

        private static readonly Color _background = new(0.02f, 0.02f, 0.07f);

        private static CameraShake _shake;

        public static void BuildTitle()
        {
            Scene scene = NewScene();
            PanelSettings hudPanel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelAssetsBuilder.HudPath);
            Transform ui = Group(scene, "[UI]");
            Transform managers = Group(scene, "[MANAGERS]");

            GameObject title = new("Title");
            title.transform.SetParent(ui, false);
            UIDocument document = AddDocument(title, hudPanel, AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TitleLayoutPath));
            TitleView view = title.AddComponent<TitleView>();
            BuilderUtil.SetObject(view, "_document", document);

            GameObject scope = new("TitleScope");
            scope.transform.SetParent(managers, false);
            scope.AddComponent<PretzelLifetimeScope>();
            TitleInstaller installer = scope.AddComponent<TitleInstaller>();
            BuilderUtil.SetObject(installer, "_title", view);

            Stars(Group(scene, "[ENVIRONMENT]"));
            Save(scene, TitleScenePath);
        }

        // Assets are loaded here, after the scene opens: opening a scene can unload objects created earlier in the same run.
        public static void BuildGame()
        {
            Scene scene = NewScene();
            DemoPrefabs prefabs = ActorPrefabsBuilder.Load();
            PanelSettings hudPanel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelAssetsBuilder.HudPath);
            PanelSettings consolePanel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelAssetsBuilder.ConsolePath);
            WaveSet waves = AssetDatabase.LoadAssetAtPath<WaveSet>(WaveAssetsBuilder.WaveSetPath);
            Transform environment = Group(scene, "[ENVIRONMENT]");
            Transform actors = Group(scene, "[ACTORS]");
            Transform managers = Group(scene, "[MANAGERS]");
            Transform ui = Group(scene, "[UI]");

            ParallaxController stars = Stars(environment);
            BunkerField bunkers = Bunkers(environment, prefabs.Bunker);

            PlayerShip player = Instantiate(prefabs.Player, actors, new Vector3(0f, -4.2f, 0f)).GetComponent<PlayerShip>();
            Ufo ufo = Instantiate(prefabs.Ufo, actors, new Vector3(0f, 3.45f, 0f)).GetComponent<Ufo>();
            InvaderFormation formation = Formation(actors, prefabs);

            ProjectileSpawner projectiles = Projectiles(managers, prefabs);
            ExplosionSpawner explosions = Explosions(managers, prefabs);
            UfoSpawner ufoSpawner = AddObject<UfoSpawner>(managers, "UfoSpawner");
            BuilderUtil.SetObject(ufoSpawner, "_ufo", ufo);
            LevelSetup setup = AddObject<LevelSetup>(managers, "LevelSetup");
            BuilderUtil.SetObject(setup, "_formation", formation);
            BuilderUtil.SetObject(setup, "_waves", waves);

            GameObject hud = new("Hud");
            hud.transform.SetParent(ui, false);
            UIDocument hudDocument = AddDocument(hud, hudPanel, AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HudLayoutPath));
            HudView hudView = hud.AddComponent<HudView>();
            BuilderUtil.SetObject(hudView, "_document", hudDocument);

            GameObject consoleObject = new("DevConsole");
            consoleObject.transform.SetParent(ui, false);
            UIDocument consoleDocument = AddDocument(consoleObject, consolePanel, null);
            DeveloperConsole console = consoleObject.AddComponent<DeveloperConsole>();
            BuilderUtil.SetObject(console, "_document", consoleDocument);

            GameObject scope = new("LevelScope");
            scope.transform.SetParent(managers, false);
            scope.AddComponent<PretzelLifetimeScope>();
            LevelInstaller installer = scope.AddComponent<LevelInstaller>();
            BuilderUtil.Edit(installer, serialized =>
            {
                BuilderUtil.Property(serialized, "_player").objectReferenceValue = player;
                BuilderUtil.Property(serialized, "_formation").objectReferenceValue = formation;
                BuilderUtil.Property(serialized, "_ufo").objectReferenceValue = ufoSpawner;
                BuilderUtil.Property(serialized, "_bunkers").objectReferenceValue = bunkers;
                BuilderUtil.Property(serialized, "_projectiles").objectReferenceValue = projectiles;
                BuilderUtil.Property(serialized, "_explosions").objectReferenceValue = explosions;
                BuilderUtil.Property(serialized, "_shake").objectReferenceValue = _shake;
                BuilderUtil.Property(serialized, "_stars").objectReferenceValue = stars;
                BuilderUtil.Property(serialized, "_hud").objectReferenceValue = hudView;
                BuilderUtil.Property(serialized, "_console").objectReferenceValue = console;
            });

            Save(scene, GameScenePath);
        }

        private static Scene NewScene()
        {
            SceneTemplateAsset template = AssetDatabase.LoadAssetAtPath<SceneTemplateAsset>(TemplatePath)
                                          ?? throw new System.InvalidOperationException($"Scene template not found at {TemplatePath}.");
            Scene scene = SceneTemplateService.Instantiate(template, false).scene;
            Camera camera = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Camera found = root.GetComponentInChildren<Camera>(true);
                if (found != null)
                {
                    camera = found;
                }
            }

            if (camera == null)
            {
                throw new System.InvalidOperationException("The Pretzel 2D scene template has no camera.");
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = _background;
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.tag = "MainCamera";

            // Cinemachine drives the camera. The fitter keeps the whole 17.8 x 10 playfield visible on any window shape, and the
            // shake component is what the game feel adds trauma to.
            CinemachineCamera rig = CameraRigBuilder.Create(CameraRigKind.TwoD);
            rig.GetComponent<PlayfieldFitter>().Configure(new Vector2(17.8f, 10f), FitMode.Contain);
            _shake = rig.GetComponent<CameraShake>();
            return scene;
        }

        private static Transform Group(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (string.Equals(root.name, name, System.StringComparison.Ordinal))
                {
                    return root.transform;
                }
            }

            GameObject created = new(name);
            SceneManager.MoveGameObjectToScene(created, scene);
            return created.transform;
        }

        private static void Save(Scene scene, string path)
        {
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new System.InvalidOperationException($"Could not save {path}.");
            }
        }

        private static UIDocument AddDocument(GameObject go, PanelSettings panel, VisualTreeAsset layout)
        {
            UIDocument document = go.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = layout;
            return document;
        }

        private static T AddObject<T>(Transform parent, string name) where T : Component
        {
            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        private static GameObject Instantiate(GameObject prefab, Transform parent, Vector3 position)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.position = position;
            return instance;
        }

        private static InvaderFormation Formation(Transform parent, DemoPrefabs prefabs)
        {
            InvaderFormation formation = AddObject<InvaderFormation>(parent, "Formation");
            SetPoolEntries(formation, "_invaders._entries", new[]
            {
                new PoolEntry((int)InvaderKind.Squid, prefabs.Squid.GetComponent<Invader>(), 0),
                new PoolEntry((int)InvaderKind.Crab, prefabs.Crab.GetComponent<Invader>(), 0),
                new PoolEntry((int)InvaderKind.Octopus, prefabs.Octopus.GetComponent<Invader>(), 0)
            });
            return formation;
        }

        private static ProjectileSpawner Projectiles(Transform parent, DemoPrefabs prefabs)
        {
            ProjectileSpawner spawner = AddObject<ProjectileSpawner>(parent, "Projectiles");
            SetPoolEntries(spawner, "_projectiles._entries", new[]
            {
                new PoolEntry((int)ProjectileKind.Bullet, prefabs.Bullet.GetComponent<Projectile>(), 4),
                new PoolEntry((int)ProjectileKind.Bomb, prefabs.Bomb.GetComponent<Projectile>(), 8)
            });
            return spawner;
        }

        private static ExplosionSpawner Explosions(Transform parent, DemoPrefabs prefabs)
        {
            ExplosionSpawner spawner = AddObject<ExplosionSpawner>(parent, "Explosions");
            BuilderUtil.SetObject(spawner, "_prefab", prefabs.Explosion.GetComponent<ExplosionFx>());
            return spawner;
        }

        private static void SetPoolEntries(Object owner, string entriesPath, IReadOnlyList<PoolEntry> entries)
        {
            BuilderUtil.Edit(owner, serialized =>
            {
                SerializedProperty list = BuilderUtil.Property(serialized, entriesPath);
                list.arraySize = entries.Count;
                for (int i = 0; i < entries.Count; i++)
                {
                    SerializedProperty entry = list.GetArrayElementAtIndex(i);
                    entry.FindPropertyRelative("key").enumValueIndex = entries[i].Key;
                    entry.FindPropertyRelative("prefab").objectReferenceValue = entries[i].Prefab;
                    entry.FindPropertyRelative("prewarm").intValue = entries[i].Prewarm;
                }
            });
        }

        private static BunkerField Bunkers(Transform parent, GameObject bunkerPrefab)
        {
            BunkerField field = AddObject<BunkerField>(parent, "Bunkers");
            foreach (float x in new[] { -5.4f, -1.8f, 1.8f, 5.4f })
            {
                Instantiate(bunkerPrefab, field.transform, new Vector3(x, -2.9f, 0f));
            }

            return field;
        }

        // Three layers of pixel-art stars drawn in Aseprite. The camera does not move, so the layers scroll by themselves: the nearer the
        // layer, the faster. The Parallax controller wraps them, so the field never ends, and a later wave speeds it all up.
        private static ParallaxController Stars(Transform parent)
        {
            DefaultArt.Load();
            GameObject root = new("Stars");
            root.transform.SetParent(parent, false);
            ParallaxController controller = root.AddComponent<ParallaxController>();
            controller.SetCamera(Camera.main != null ? Camera.main.transform : null);

            AddStarLayer(root.transform, "stars_far", -30, 0.35f);
            AddStarLayer(root.transform, "stars_mid", -20, 0.9f);
            AddStarLayer(root.transform, "stars_near", -10, 1.8f);
            return controller;
        }

        private static void AddStarLayer(Transform parent, string tile, int sortingOrder, float unitsPerSecond)
        {
            Sprite sprite = ArtImporter.LoadStars(tile);
            GameObject layerObject = new(tile);
            layerObject.transform.SetParent(parent, false);

            // 4 x 4 tiles of 16 units: enough for any window shape the playfield fitter can produce.
            SpriteRenderer renderer = layerObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = DefaultArt.Unlit;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = sprite.bounds.size * 4f;
            renderer.sortingOrder = sortingOrder;

            ParallaxLayer layer = layerObject.AddComponent<ParallaxLayer>();
            layer.Configure(Vector2.zero, repeatX: true, repeatY: true);
            layer.SetAutoScroll(new Vector2(0f, -unitsPerSecond));
        }

        private sealed class PoolEntry
        {
            public readonly int Key;
            public readonly Object Prefab;
            public readonly int Prewarm;

            public PoolEntry(int key, Object prefab, int prewarm)
            {
                Key = key;
                Prefab = prefab;
                Prewarm = prewarm;
            }
        }
    }
}
