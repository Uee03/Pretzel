using System.Collections.Generic;
using UnityEngine;

namespace Pretzel.Samples.Console
{
    /// <summary>
    /// A tiny arena that builds itself at runtime (primitives only, any render pipeline): a player, chasing enemies, gold and a
    /// HUD. It exists so console commands have something real to change; see <see cref="ArenaCommands"/>.
    /// </summary>
    public sealed class ArenaGame : MonoBehaviour
    {
        private const float SpawnRadius = 12f;

        [SerializeField, Min(0)] private int _startingEnemies = 3;

        private readonly List<Enemy> _enemies = new();
        private float _nextSpawn;
        private Material _groundMaterial;
        private Material _playerMaterial;
        private Material _enemyMaterial;

        public ArenaTuning Tuning { get; } = new();

        public PlayerHealth Player { get; private set; }

        public IReadOnlyList<Enemy> Enemies => _enemies;

        public int Gold { get; private set; }

        public int Kills { get; private set; }

        /// <summary>True while the console has focus, so typing does not move the player.</summary>
        public bool InputBlocked { get; set; }

        private void Awake()
        {
            BuildWorld();
            for (int i = 0; i < _startingEnemies; i++)
            {
                SpawnEnemy();
            }
        }

        private void OnDestroy()
        {
            // The materials are copies owned by this sample, so they are destroyed with it.
            Destroy(_groundMaterial);
            Destroy(_playerMaterial);
            Destroy(_enemyMaterial);
        }

        private void Update()
        {
            if (Player.IsDead || Tuning.SpawnInterval <= 0f || Time.time < _nextSpawn)
            {
                return;
            }

            _nextSpawn = Time.time + Tuning.SpawnInterval;
            if (_enemies.Count < Tuning.MaxEnemies)
            {
                SpawnEnemy();
            }
        }

        public Enemy SpawnEnemy()
        {
            float angle = Random.value * Mathf.PI * 2f;
            Vector3 position = Player.transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * SpawnRadius;
            position.y = 0.5f;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Enemy";
            body.transform.SetParent(transform, false);
            body.transform.position = position;
            Destroy(body.GetComponent<Collider>());
            Renderer renderer = body.GetComponent<Renderer>();
            _enemyMaterial ??= CreateMaterial(renderer, new Color(0.85f, 0.2f, 0.2f));
            renderer.sharedMaterial = _enemyMaterial;

            Enemy enemy = body.AddComponent<Enemy>();
            enemy.Initialize(this);
            _enemies.Add(enemy);
            return enemy;
        }

        public Enemy FindNearestEnemy(Vector3 from, float radius)
        {
            Enemy nearest = null;
            float best = radius * radius;
            foreach (Enemy enemy in _enemies)
            {
                float distance = (enemy.transform.position - from).sqrMagnitude;
                if (distance <= best)
                {
                    best = distance;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        /// <summary>Removes an enemy the player defeated and pays the gold reward.</summary>
        public void Defeat(Enemy enemy)
        {
            if (Remove(enemy))
            {
                Kills++;
                Gold += Mathf.RoundToInt(Tuning.GoldPerKill);
            }
        }

        /// <summary>Removes every enemy without a reward and returns how many there were.</summary>
        public int RemoveAllEnemies()
        {
            int count = _enemies.Count;
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                Remove(_enemies[i]);
            }

            return count;
        }

        public void AddGold(int amount)
        {
            Gold = Mathf.Max(0, Gold + amount);
        }

        public void Teleport(float x, float z)
        {
            Player.transform.position = new Vector3(x, 0.5f, z);
        }

        public void ResetArena()
        {
            RemoveAllEnemies();
            Player.Revive();
            Gold = 0;
            Kills = 0;
            Teleport(0f, 0f);
        }

        private static Material CreateMaterial(Renderer source, Color color)
        {
            return new Material(source.sharedMaterial) { color = color };
        }

        private bool Remove(Enemy enemy)
        {
            if (!_enemies.Remove(enemy))
            {
                return false;
            }

            if (enemy)
            {
                Destroy(enemy.gameObject);
            }

            return true;
        }

        private void BuildWorld()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(transform, false);
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
            Destroy(ground.GetComponent<Collider>());
            Renderer groundRenderer = ground.GetComponent<Renderer>();
            _groundMaterial = CreateMaterial(groundRenderer, new Color(0.25f, 0.28f, 0.32f));
            groundRenderer.sharedMaterial = _groundMaterial;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Player";
            body.transform.SetParent(transform, false);
            body.transform.position = new Vector3(0f, 0.5f, 0f);
            Destroy(body.GetComponent<Collider>());
            Renderer playerRenderer = body.GetComponent<Renderer>();
            _playerMaterial = CreateMaterial(playerRenderer, new Color(0.2f, 0.6f, 1f));
            playerRenderer.sharedMaterial = _playerMaterial;
            Player = body.AddComponent<PlayerHealth>();
            body.AddComponent<PlayerController>().Initialize(this);

            if (!Camera.main)
            {
                GameObject cameraObject = new("Arena Camera") { tag = "MainCamera" };
                cameraObject.transform.SetParent(transform, false);
                cameraObject.AddComponent<Camera>();
                cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 22f, -9f), Quaternion.Euler(70f, 0f, 0f));
            }

            if (!FindAnyObjectByType<Light>())
            {
                GameObject lightObject = new("Arena Light");
                lightObject.transform.SetParent(transform, false);
                lightObject.AddComponent<Light>().type = LightType.Directional;
                lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
        }
    }
}
