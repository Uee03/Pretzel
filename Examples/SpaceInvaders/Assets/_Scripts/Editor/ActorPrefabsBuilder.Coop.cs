using Pretzel.Multiplayer;
using Pretzel.Multiplayer.Editor;
using SpaceInvaders.Coop;
using SpaceInvaders.Gameplay;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace SpaceInvaders.Editor
{
    /// <summary>The network prefabs of the co-op mode: a ship for each player, and the bullet and bomb that the host spawns from pools.</summary>
    internal static partial class ActorPrefabsBuilder
    {
        public const string CoopShipName = "CoopShip";
        public const string CoopBulletName = "CoopBullet";
        public const string CoopBombName = "CoopBomb";

        public static void BuildCoopPrefabs()
        {
            BuilderUtil.EnsureFolder(Folder);
            DefaultArt.Load();
            _rounded = DefaultArt.Rounded;
            _circle = DefaultArt.Circle;
            _unlit = DefaultArt.Unlit;

            BuildCoopShip();
            BuildCoopProjectile(CoopBulletName, new Vector2(0.07f, 0.38f), Color.white, Team.Player, Vector2.up, 13f);
            BuildCoopProjectile(CoopBombName, new Vector2(0.12f, 0.3f), new Color(1f, 0.6f, 0.2f), Team.Invaders, Vector2.down, 5.5f);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Loads a co-op prefab by path. Call it right before use: editor asset unloading can leave earlier references destroyed.</summary>
        public static GameObject LoadCoop(string name)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/{name}.prefab")
                   ?? throw new System.InvalidOperationException($"Prefab {name} was not found in {Folder}. Run Space Invaders > Build Co-op.");
        }

        // The white ship is tinted by slot at runtime, so the two players can tell their cannons apart.
        private static void BuildCoopShip()
        {
            Color white = Color.white;
            GameObject root = new(CoopShipName);
            Rect("Base", root.transform, new Vector2(0f, -0.1f), new Vector2(0.9f, 0.22f), white, 2);
            Rect("Mid", root.transform, new Vector2(0f, 0.08f), new Vector2(0.6f, 0.2f), white, 2);
            Rect("Turret", root.transform, new Vector2(0f, 0.26f), new Vector2(0.14f, 0.24f), white, 2);
            GameObject muzzle = new("Muzzle");
            muzzle.transform.SetParent(root.transform, false);
            muzzle.transform.localPosition = new Vector3(0f, 0.42f, 0f);

            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            AddTrigger(root, new Vector2(0.9f, 0.55f), new Vector2(0f, 0.05f));

            root.AddComponent<NetworkObject>();
            root.AddComponent<NetworkPlayerIdentity>();
            NetworkTransform networkTransform = root.AddComponent<NetworkTransform>();
            networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            networkTransform.Interpolate = true;
            networkTransform.PositionThreshold = 0.01f;
            networkTransform.SyncPositionY = false;
            networkTransform.SyncPositionZ = false;
            networkTransform.SyncRotAngleX = false;
            networkTransform.SyncRotAngleY = false;
            networkTransform.SyncRotAngleZ = false;
            networkTransform.SyncScaleX = false;
            networkTransform.SyncScaleY = false;
            networkTransform.SyncScaleZ = false;

            CoopShip ship = root.AddComponent<CoopShip>();
            BuilderUtil.SetObject(ship, "_muzzle", muzzle.transform);
            BuilderUtil.SetObjects(ship, "_renderers", ToObjects(root.GetComponentsInChildren<SpriteRenderer>()));
            NetworkPrefabAssetUtility.SavePrefab(root, $"{Folder}/{CoopShipName}.prefab");
        }

        private static void BuildCoopProjectile(string name, Vector2 size, Color color, Team team, Vector2 direction, float speed)
        {
            GameObject root = new(name);
            Rect("Shape", root.transform, Vector2.zero, size, color, 4);
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            AddTrigger(root, size + new Vector2(0.04f, 0.04f), Vector2.zero);
            root.AddComponent<NetworkObject>();

            CoopProjectile projectile = root.AddComponent<CoopProjectile>();
            BuilderUtil.Edit(projectile, serialized =>
            {
                BuilderUtil.Property(serialized, "_team").enumValueIndex = (int)team;
                BuilderUtil.Property(serialized, "_direction").vector2Value = direction;
                BuilderUtil.Property(serialized, "_speed").floatValue = speed;
            });
            NetworkPrefabAssetUtility.SavePrefab(root, $"{Folder}/{name}.prefab");
        }
    }
}
