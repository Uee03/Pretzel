using System.Collections.Generic;
using SpaceInvaders.Gameplay;
using UnityEditor;
using UnityEngine;

namespace SpaceInvaders.Editor
{
    /// <summary>
    /// Builds every actor prefab from Unity's built-in UI sprites (a rounded rectangle and a circle), tinted and composed into shapes.
    /// No imported art is needed, so the demo works in any project that has the default resources.
    /// </summary>
    internal static class ActorPrefabsBuilder
    {
        public const string Folder = "Assets/_Project/Prefabs";

        private static readonly Color _eye = new(0.03f, 0.03f, 0.08f);
        private static readonly string[] _bunkerShape = { " XXXXX ", "XXXXXXX", "XXXXXXX", "XXX XXX" };

        private static Sprite _rounded;
        private static Sprite _circle;
        private static Material _unlit;

        public static void Build()
        {
            BuilderUtil.EnsureFolder(Folder);
            DefaultArt.Load();
            _rounded = DefaultArt.Rounded;
            _circle = DefaultArt.Circle;
            _unlit = DefaultArt.Unlit;

            BuildPlayer();
            BuildSquid();
            BuildCrab();
            BuildOctopus();
            BuildProjectile("Bullet", new Vector2(0.07f, 0.38f), Color.white, Team.Player, Vector2.up, 13f);
            BuildProjectile("Bomb", new Vector2(0.12f, 0.3f), new Color(1f, 0.6f, 0.2f), Team.Invaders, Vector2.down, 5.5f);
            BuildExplosion();
            BuildUfo();
            BuildBunker();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Loads the prefabs by path. Call it right before use: editor asset unloading can leave earlier references destroyed.</summary>
        public static DemoPrefabs Load()
        {
            return new DemoPrefabs
            {
                Player = LoadPrefab("Player"),
                Squid = LoadPrefab("Invader_Squid"),
                Crab = LoadPrefab("Invader_Crab"),
                Octopus = LoadPrefab("Invader_Octopus"),
                Bullet = LoadPrefab("Bullet"),
                Bomb = LoadPrefab("Bomb"),
                Explosion = LoadPrefab("Explosion"),
                Ufo = LoadPrefab("Ufo"),
                Bunker = LoadPrefab("Bunker")
            };
        }

        private static GameObject LoadPrefab(string name)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/{name}.prefab")
                   ?? throw new System.InvalidOperationException($"Prefab {name} was not found in {Folder}.");
        }

        private static GameObject BuildPlayer()
        {
            Color green = new(0.4f, 1f, 0.5f);
            GameObject root = new("Player");
            Rect("Base", root.transform, new Vector2(0f, -0.1f), new Vector2(0.9f, 0.22f), green, 2);
            Rect("Mid", root.transform, new Vector2(0f, 0.08f), new Vector2(0.6f, 0.2f), green, 2);
            Rect("Turret", root.transform, new Vector2(0f, 0.26f), new Vector2(0.14f, 0.24f), green, 2);
            GameObject muzzle = new("Muzzle");
            muzzle.transform.SetParent(root.transform, false);
            muzzle.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            AddTrigger(root, new Vector2(0.9f, 0.55f), new Vector2(0f, 0.05f));

            PlayerShip ship = root.AddComponent<PlayerShip>();
            BuilderUtil.SetObject(ship, "_muzzle", muzzle.transform);
            BuilderUtil.SetObjects(ship, "_renderers", ToObjects(root.GetComponentsInChildren<SpriteRenderer>()));
            return Save(root, "Player");
        }

        private static GameObject BuildSquid()
        {
            Color color = new(1f, 0.35f, 0.85f);
            GameObject root = new("Invader_Squid");
            Rect("Head", root.transform, new Vector2(0f, 0.06f), new Vector2(0.42f, 0.34f), color, 3);
            Rect("Crest", root.transform, new Vector2(0f, 0.24f), new Vector2(0.26f, 0.12f), color, 3);
            AddEyes(root.transform, 0.09f, 0.08f);
            Transform frameA = Frame(root.transform, "FrameA", color, new[] { -0.17f, -0.06f, 0.06f, 0.17f }, -0.17f, new Vector2(0.06f, 0.14f));
            Transform frameB = Frame(root.transform, "FrameB", color, new[] { -0.2f, -0.07f, 0.07f, 0.2f }, -0.19f, new Vector2(0.06f, 0.1f));
            return FinishInvader(root, InvaderKind.Squid, frameA, frameB, new Vector2(0.5f, 0.6f));
        }

        private static GameObject BuildCrab()
        {
            Color color = new(0.3f, 0.9f, 1f);
            GameObject root = new("Invader_Crab");
            Rect("Body", root.transform, Vector2.zero, new Vector2(0.6f, 0.34f), color, 3);
            Rect("Crest", root.transform, new Vector2(0f, 0.2f), new Vector2(0.36f, 0.1f), color, 3);
            Rect("AntennaL", root.transform, new Vector2(-0.16f, 0.3f), new Vector2(0.05f, 0.14f), color, 3);
            Rect("AntennaR", root.transform, new Vector2(0.16f, 0.3f), new Vector2(0.05f, 0.14f), color, 3);
            AddEyes(root.transform, 0.12f, 0.04f);

            Transform frameA = new GameObject("FrameA").transform;
            frameA.SetParent(root.transform, false);
            Rect("ClawL", frameA, new Vector2(-0.36f, 0.1f), new Vector2(0.1f, 0.3f), color, 3);
            Rect("ClawR", frameA, new Vector2(0.36f, 0.1f), new Vector2(0.1f, 0.3f), color, 3);
            Rect("LegL", frameA, new Vector2(-0.2f, -0.22f), new Vector2(0.07f, 0.12f), color, 3);
            Rect("LegR", frameA, new Vector2(0.2f, -0.22f), new Vector2(0.07f, 0.12f), color, 3);

            Transform frameB = new GameObject("FrameB").transform;
            frameB.SetParent(root.transform, false);
            Rect("ClawL", frameB, new Vector2(-0.36f, -0.08f), new Vector2(0.1f, 0.3f), color, 3);
            Rect("ClawR", frameB, new Vector2(0.36f, -0.08f), new Vector2(0.1f, 0.3f), color, 3);
            Rect("LegL", frameB, new Vector2(-0.28f, -0.2f), new Vector2(0.07f, 0.1f), color, 3);
            Rect("LegR", frameB, new Vector2(0.28f, -0.2f), new Vector2(0.07f, 0.1f), color, 3);
            return FinishInvader(root, InvaderKind.Crab, frameA, frameB, new Vector2(0.8f, 0.55f));
        }

        private static GameObject BuildOctopus()
        {
            Color color = new(0.4f, 1f, 0.45f);
            GameObject root = new("Invader_Octopus");
            Rect("Body", root.transform, new Vector2(0f, 0.02f), new Vector2(0.72f, 0.3f), color, 3);
            Rect("Head", root.transform, new Vector2(0f, 0.2f), new Vector2(0.46f, 0.14f), color, 3);
            AddEyes(root.transform, 0.14f, 0.04f);
            Transform frameA = Frame(root.transform, "FrameA", color, new[] { -0.28f, -0.09f, 0.09f, 0.28f }, -0.2f, new Vector2(0.1f, 0.12f));
            Transform frameB = Frame(root.transform, "FrameB", color, new[] { -0.32f, -0.13f, 0.13f, 0.32f }, -0.2f, new Vector2(0.1f, 0.12f));
            return FinishInvader(root, InvaderKind.Octopus, frameA, frameB, new Vector2(0.78f, 0.55f));
        }

        private static GameObject BuildProjectile(string name, Vector2 size, Color color, Team team, Vector2 direction, float speed)
        {
            GameObject root = new(name);
            Rect("Shape", root.transform, Vector2.zero, size, color, 4);
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            AddTrigger(root, size + new Vector2(0.04f, 0.04f), Vector2.zero);

            Projectile projectile = root.AddComponent<Projectile>();
            BuilderUtil.Edit(projectile, serialized =>
            {
                BuilderUtil.Property(serialized, "_team").enumValueIndex = (int)team;
                BuilderUtil.Property(serialized, "_direction").vector2Value = direction;
                BuilderUtil.Property(serialized, "_speed").floatValue = speed;
            });
            return Save(root, name);
        }

        private static GameObject BuildExplosion()
        {
            GameObject root = new("Explosion");
            SpriteRenderer renderer = Disc("Disc", root.transform, Vector2.zero, Vector2.one, Color.white, 5);
            ExplosionFx fx = root.AddComponent<ExplosionFx>();
            BuilderUtil.SetObject(fx, "_renderer", renderer);
            return Save(root, "Explosion");
        }

        private static GameObject BuildUfo()
        {
            GameObject root = new("Ufo");
            Disc("Hull", root.transform, Vector2.zero, new Vector2(0.9f, 0.34f), new Color(1f, 0.3f, 0.35f), 3);
            Disc("Dome", root.transform, new Vector2(0f, 0.13f), new Vector2(0.4f, 0.28f), new Color(1f, 0.62f, 0.66f), 2);
            foreach (float x in new[] { -0.26f, 0f, 0.26f })
            {
                Disc("Window", root.transform, new Vector2(x, -0.01f), new Vector2(0.1f, 0.1f), new Color(1f, 0.9f, 0.4f), 4);
            }

            AddTrigger(root, new Vector2(0.9f, 0.36f), Vector2.zero);
            root.AddComponent<Ufo>();
            return Save(root, "Ufo");
        }

        private static GameObject BuildBunker()
        {
            Color color = new(0.3f, 0.9f, 0.4f);
            Vector2 block = new(0.24f, 0.2f);
            GameObject root = new("Bunker");
            for (int row = 0; row < _bunkerShape.Length; row++)
            {
                for (int column = 0; column < _bunkerShape[row].Length; column++)
                {
                    if (_bunkerShape[row][column] != 'X')
                    {
                        continue;
                    }

                    Vector2 position = new((column - 3) * block.x, (1.5f - row) * block.y);
                    SpriteRenderer renderer = Rect("Block", root.transform, position, block, color, 1);
                    AddTrigger(renderer.gameObject, block, Vector2.zero);
                    BunkerBlock brick = renderer.gameObject.AddComponent<BunkerBlock>();
                    BuilderUtil.SetObject(brick, "_renderer", renderer);
                }
            }

            return Save(root, "Bunker");
        }

        private static Transform Frame(Transform parent, string name, Color color, float[] xs, float y, Vector2 size)
        {
            Transform frame = new GameObject(name).transform;
            frame.SetParent(parent, false);
            for (int i = 0; i < xs.Length; i++)
            {
                Rect("Limb" + i, frame, new Vector2(xs[i], y), size, color, 3);
            }

            return frame;
        }

        private static void AddEyes(Transform parent, float x, float y)
        {
            Disc("EyeL", parent, new Vector2(-x, y), new Vector2(0.1f, 0.1f), _eye, 4);
            Disc("EyeR", parent, new Vector2(x, y), new Vector2(0.1f, 0.1f), _eye, 4);
        }

        private static GameObject FinishInvader(GameObject root, InvaderKind kind, Transform frameA, Transform frameB, Vector2 colliderSize)
        {
            AddTrigger(root, colliderSize, Vector2.zero);
            Invader invader = root.AddComponent<Invader>();
            BuilderUtil.SetInt(invader, "_kind", (int)kind);
            BuilderUtil.SetObjects(invader, "_frames", new Object[] { frameA.gameObject, frameB.gameObject });
            return Save(root, "Invader_" + kind);
        }

        private static SpriteRenderer Rect(string name, Transform parent, Vector2 position, Vector2 size, Color color, int order)
        {
            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = _rounded;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            renderer.color = color;
            renderer.sortingOrder = order;
            renderer.sharedMaterial = _unlit;
            return renderer;
        }

        private static SpriteRenderer Disc(string name, Transform parent, Vector2 position, Vector2 size, Color color, int order)
        {
            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            Vector3 native = _circle.bounds.size;
            go.transform.localScale = new Vector3(size.x / native.x, size.y / native.y, 1f);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = _circle;
            renderer.color = color;
            renderer.sortingOrder = order;
            renderer.sharedMaterial = _unlit;
            return renderer;
        }

        private static void AddTrigger(GameObject go, Vector2 size, Vector2 offset)
        {
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = size;
            collider.offset = offset;
        }

        private static List<Object> ToObjects(SpriteRenderer[] renderers)
        {
            List<Object> objects = new();
            foreach (SpriteRenderer renderer in renderers)
            {
                objects.Add(renderer);
            }

            return objects;
        }

        private static GameObject Save(GameObject root, string name)
        {
            string path = $"{Folder}/{name}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }
    }
}
