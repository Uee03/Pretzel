using Pretzel.Integrations.VContainer;
using Pretzel.Multiplayer;
using Pretzel.Multiplayer.Editor;
using Pretzel.Parallax;
using SpaceInvaders.Coop;
using SpaceInvaders.Gameplay;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace SpaceInvaders.Editor
{
    /// <summary>Builds the Coop scene: the game's world, a NetworkManager with the Pretzel bootstrap, the Pretzel menu, the co-op HUD and the scope that wires them.</summary>
    internal static partial class SceneBuilder
    {
        public const string CoopScenePath = "Assets/_Project/Scenes/Coop.unity";

        private const string NetworkFolder = "Assets/_Project/Network";
        private const string CoopHudLayoutPath = "Assets/_Project/UI/Layouts/CoopHud.uxml";
        private const string NetworkPrefabListName = "SpaceInvadersNetworkPrefabs.asset";

        // Assets are loaded here, after the scene opens: opening a scene can unload objects created earlier in the same run.
        public static void BuildCoop()
        {
            BuilderUtil.EnsureFolder(NetworkFolder);
            Scene scene = NewScene();
            DemoPrefabs prefabs = ActorPrefabsBuilder.Load();
            GameObject ship = ActorPrefabsBuilder.LoadCoop(ActorPrefabsBuilder.CoopShipName);
            GameObject bullet = ActorPrefabsBuilder.LoadCoop(ActorPrefabsBuilder.CoopBulletName);
            GameObject bomb = ActorPrefabsBuilder.LoadCoop(ActorPrefabsBuilder.CoopBombName);
            PanelSettings hudPanel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelAssetsBuilder.HudPath);
            WaveSet waves = AssetDatabase.LoadAssetAtPath<WaveSet>(WaveAssetsBuilder.WaveSetPath);
            Transform environment = Group(scene, "[ENVIRONMENT]");
            Transform actors = Group(scene, "[ACTORS]");
            Transform managers = Group(scene, "[MANAGERS]");
            Transform ui = Group(scene, "[UI]");

            ParallaxController stars = Stars(environment);
            BunkerField bunkers = Bunkers(environment, prefabs.Bunker);
            Ufo ufo = Instantiate(prefabs.Ufo, actors, new Vector3(0f, 3.45f, 0f)).GetComponent<Ufo>();
            InvaderFormation formation = Formation(actors, prefabs);
            ExplosionSpawner explosions = Explosions(managers, prefabs);
            UfoSpawner ufoSpawner = AddObject<UfoSpawner>(managers, "UfoSpawner");
            BuilderUtil.SetObject(ufoSpawner, "_ufo", ufo);
            LevelSetup setup = AddObject<LevelSetup>(managers, "LevelSetup");
            BuilderUtil.SetObject(setup, "_formation", formation);
            BuilderUtil.SetObject(setup, "_waves", waves);

            PretzelNetworkBootstrap bootstrap = BuildNetwork(scene, actors, ship, bullet, bomb);

            // NGO rejects a NetworkManager under another object, so the network objects stay at the scene root.
            GameObject hub = new("Coop");
            SceneManager.MoveGameObjectToScene(hub, scene);
            hub.AddComponent<NetworkObject>();
            BuilderUtil.SetObject(hub.AddComponent<CoopNetworkLifetime>(), "_bootstrap", bootstrap);
            CoopMatch match = hub.AddComponent<CoopMatch>();
            CoopWorldReplicator world = hub.AddComponent<CoopWorldReplicator>();
            BuilderUtil.SetObject(world, "_formation", formation);
            BuilderUtil.SetObject(world, "_ufo", ufo);
            BuilderUtil.SetObject(world, "_bunkers", bunkers);

            CoopProjectileSpawner projectiles = AddObject<CoopProjectileSpawner>(managers, "Projectiles");
            BuilderUtil.SetObject(projectiles, "_bootstrap", bootstrap);
            BuilderUtil.SetObject(projectiles, "_bulletPrefab", bullet);
            BuilderUtil.SetObject(projectiles, "_bombPrefab", bomb);

            // The Pretzel menu sits under the HUD, so the leave button and the game-over panel stay on top of it.
            MultiplayerSceneBuilder.CreateMenu(ui.gameObject, NetworkFolder);
            PanelSettings menuPanel = MultiplayerSceneBuilder.GetOrCreatePanelSettings(NetworkFolder);
            menuPanel.sortingOrder = -10;
            EditorUtility.SetDirty(menuPanel);

            GameObject hud = new("CoopHud");
            hud.transform.SetParent(ui, false);
            UIDocument hudDocument = AddDocument(hud, hudPanel, AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CoopHudLayoutPath));
            CoopHudView hudView = hud.AddComponent<CoopHudView>();
            BuilderUtil.SetObject(hudView, "_document", hudDocument);

            GameObject scope = new("CoopScope");
            scope.transform.SetParent(managers, false);
            scope.AddComponent<PretzelLifetimeScope>();
            CoopLevelInstaller installer = scope.AddComponent<CoopLevelInstaller>();
            BuilderUtil.Edit(installer, serialized =>
            {
                BuilderUtil.Property(serialized, "_bootstrap").objectReferenceValue = bootstrap;
                BuilderUtil.Property(serialized, "_match").objectReferenceValue = match;
                BuilderUtil.Property(serialized, "_world").objectReferenceValue = world;
                BuilderUtil.Property(serialized, "_projectiles").objectReferenceValue = projectiles;
                BuilderUtil.Property(serialized, "_formation").objectReferenceValue = formation;
                BuilderUtil.Property(serialized, "_ufo").objectReferenceValue = ufoSpawner;
                BuilderUtil.Property(serialized, "_bunkers").objectReferenceValue = bunkers;
                BuilderUtil.Property(serialized, "_explosions").objectReferenceValue = explosions;
                BuilderUtil.Property(serialized, "_shake").objectReferenceValue = _shake;
                BuilderUtil.Property(serialized, "_stars").objectReferenceValue = stars;
                BuilderUtil.Property(serialized, "_hud").objectReferenceValue = hudView;
            });

            Save(scene, CoopScenePath);
            RefreshInScenePlacedHashes();
        }

        private static PretzelNetworkBootstrap BuildNetwork(Scene scene, Transform actors, GameObject ship, GameObject bullet, GameObject bomb)
        {
            GameObject networkManagerObject = MultiplayerSceneBuilder.CreateNetworkManager(null, NetworkFolder);
            SceneManager.MoveGameObjectToScene(networkManagerObject, scene);
            NetworkManager networkManager = networkManagerObject.GetComponent<NetworkManager>();
            NetworkPrefabListUtility.UseDedicatedList(networkManager, NetworkFolder, NetworkPrefabListName);
            NetworkPrefabListUtility.Register(networkManager, ship, NetworkFolder);
            NetworkPrefabListUtility.Register(networkManager, bullet, NetworkFolder);
            NetworkPrefabListUtility.Register(networkManager, bomb, NetworkFolder);

            PretzelNetworkBootstrap bootstrap = networkManagerObject.GetComponent<PretzelNetworkBootstrap>();
            NetworkBootstrapEditorUtility.SetPlayerPrefab(bootstrap, ship);
            NetworkBootstrapEditorUtility.AddPooledPrefab(bootstrap, bullet, 4, 16);
            NetworkBootstrapEditorUtility.AddPooledPrefab(bootstrap, bomb, 8, 16);
            NetworkBootstrapEditorUtility.SetSpawnPoints(bootstrap, SpawnPoints(actors));

            // Two pilots: one slot each, and at most two on one machine for couch play.
            BuilderUtil.SetInt(bootstrap, "_settings._maxPlayers", 2);
            BuilderUtil.SetInt(bootstrap, "_settings._maxLocalPlayersPerClient", 2);
            return bootstrap;
        }

        private static Transform[] SpawnPoints(Transform parent)
        {
            GameObject root = new("Spawn Points");
            root.transform.SetParent(parent, false);
            Transform[] points = new Transform[2];
            float[] xs = { -3f, 3f };
            for (int i = 0; i < points.Length; i++)
            {
                GameObject point = new("Spawn " + (i + 1));
                point.transform.SetParent(root.transform, false);
                point.transform.position = new Vector3(xs[i], -4.2f, 0f);
                points[i] = point.transform;
            }

            return points;
        }

        // NGO writes the GlobalObjectIdHash of a scene-placed NetworkObject in OnValidate, which needs a saved scene: reopen it once and save again.
        private static void RefreshInScenePlacedHashes()
        {
            Scene reopened = EditorSceneManager.OpenScene(CoopScenePath, OpenSceneMode.Single);
            EditorSceneManager.MarkSceneDirty(reopened);
            EditorSceneManager.SaveScene(reopened);
        }
    }
}
