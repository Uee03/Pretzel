using System;
using System.Collections.Generic;
using System.Text;
using Pretzel.Pooling;
using UnityEngine;

namespace Pretzel.Samples.Pooling.Waves
{
    /// <summary>
    /// Wave spawner on a keyed <see cref="PoolFactory{TKey,T}"/>. It shows the parts a real game needs: prewarmed pools sized for the
    /// peak, a cap on live enemies, a spawn that fills the instance after taking it, a return callback that keeps the live list
    /// correct, and pool counts you can read to tune the prewarm numbers. The templates are built in code, so no prefabs are needed;
    /// in a project, assign real prefabs under <b>Enemies &gt; Entries</b> and the code uses those instead.
    /// </summary>
    public sealed class WaveSpawner : MonoBehaviour
    {
        private const float SpawnRadius = 14f;

        [SerializeField] private PoolFactory<EnemyKind, WaveEnemy> _enemies = new();
        [SerializeField, Min(1)] private int _maxAlive = 60;
        [SerializeField, Min(1)] private int _firstWaveSize = 8;
        [SerializeField, Min(0.01f)] private float _spawnInterval = 0.15f;
        [SerializeField, Min(0.1f)] private float _waveDelay = 3f;
        [SerializeField, Min(0.01f)] private float _defenseInterval = 0.12f;
        [SerializeField, Min(0.1f)] private float _defenseDamage = 1f;

        private readonly List<WaveEnemy> _alive = new();
        private readonly List<Material> _materials = new();
        private readonly StringBuilder _report = new();
        private Action<WaveEnemy, bool> _onReturned;
        private float _nextSpawn;
        private float _nextWave;
        private float _nextDefense;
        private int _toSpawn;

        public int Wave { get; private set; }

        public int Alive => _alive.Count;

        public int Defeated { get; private set; }

        public int Escaped { get; private set; }

        private void Awake()
        {
            _onReturned = OnEnemyReturned;
            if (_enemies.Entries.Count == 0)
            {
                AddRuntimeTemplates();
            }

            _enemies.Initialize(transform);
            EnsureCamera();
        }

        private void Update()
        {
            if (_toSpawn == 0 && Time.time >= _nextWave)
            {
                Wave++;
                _toSpawn = _firstWaveSize + (Wave - 1) * 4;
            }

            if (_toSpawn > 0 && Time.time >= _nextSpawn && _alive.Count < _maxAlive)
            {
                SpawnOne(_toSpawn);
                _nextSpawn = Time.time + _spawnInterval;
                _toSpawn--;
                if (_toSpawn == 0)
                {
                    _nextWave = Time.time + _waveDelay;
                }
            }

            if (Time.time >= _nextDefense)
            {
                _nextDefense = Time.time + _defenseInterval;
                DefendCentre();
            }
        }

        private void OnDestroy()
        {
            _enemies.Dispose();
            foreach (Material material in _materials)
            {
                Destroy(material);
            }
        }

        /// <summary>One line per pool: how many are in use, idle, and created in total.</summary>
        public string DescribePools()
        {
            _report.Clear();
            foreach (EnemyKind kind in _enemies.Keys)
            {
                ComponentPool<WaveEnemy> pool = _enemies.GetPool(kind);
                _report.Append(kind).Append(": ").Append(pool.CountActive).Append(" active, ").Append(pool.CountInactive).Append(" idle, ").Append(pool.CountAll).AppendLine(" created");
            }

            return _report.ToString();
        }

        private void SpawnOne(int index)
        {
            EnemyKind kind = index % 5 == 0 && Wave >= 2 ? EnemyKind.Brute : index % 2 == 0 ? EnemyKind.Runner : EnemyKind.Grunt;
            float angle = UnityEngine.Random.value * Mathf.PI * 2f;
            Vector3 position = new(Mathf.Cos(angle) * SpawnRadius, 0.5f, Mathf.Sin(angle) * SpawnRadius);

            WaveEnemy enemy = _enemies.Spawn(kind, position, Quaternion.identity);
            switch (kind)
            {
                case EnemyKind.Brute:
                    enemy.Configure(kind, 1.2f, 8f, _onReturned);
                    break;
                case EnemyKind.Runner:
                    enemy.Configure(kind, 4f, 1.5f, _onReturned);
                    break;
                default:
                    enemy.Configure(kind, 2f, 3f, _onReturned);
                    break;
            }

            _alive.Add(enemy);
        }

        private void DefendCentre()
        {
            WaveEnemy nearest = null;
            float best = float.MaxValue;
            foreach (WaveEnemy enemy in _alive)
            {
                float distance = enemy.transform.position.sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    nearest = enemy;
                }
            }

            // Hit after the loop: a defeated enemy returns to its pool and removes itself from the list.
            if (nearest)
            {
                nearest.Hit(_defenseDamage);
            }
        }

        private void OnEnemyReturned(WaveEnemy enemy, bool arrived)
        {
            _alive.Remove(enemy);
            if (arrived)
            {
                Escaped++;
            }
            else
            {
                Defeated++;
            }
        }

        private void AddRuntimeTemplates()
        {
            // An inactive parent keeps the templates from running Awake or Update.
            Transform templates = new GameObject("[Templates]").transform;
            templates.SetParent(transform, false);
            templates.gameObject.SetActive(false);

            _enemies.AddEntry(EnemyKind.Grunt, CreateTemplate(templates, EnemyKind.Grunt, new Color(0.9f, 0.8f, 0.2f), 1f), 16);
            _enemies.AddEntry(EnemyKind.Runner, CreateTemplate(templates, EnemyKind.Runner, new Color(0.2f, 0.8f, 0.9f), 0.7f), 12);
            _enemies.AddEntry(EnemyKind.Brute, CreateTemplate(templates, EnemyKind.Brute, new Color(0.9f, 0.3f, 0.3f), 1.6f), 6);
        }

        private WaveEnemy CreateTemplate(Transform parent, EnemyKind kind, Color color, float scale)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = kind.ToString();
            body.transform.SetParent(parent, false);
            body.transform.localScale = Vector3.one * scale;
            Destroy(body.GetComponent<Collider>());

            Renderer renderer = body.GetComponent<Renderer>();
            Material material = new(renderer.sharedMaterial) { color = color };
            renderer.sharedMaterial = material;
            _materials.Add(material);
            return body.AddComponent<WaveEnemy>();
        }

        private void EnsureCamera()
        {
            if (!Camera.main)
            {
                GameObject cameraObject = new("Wave Camera") { tag = "MainCamera" };
                cameraObject.transform.SetParent(transform, false);
                cameraObject.AddComponent<Camera>();
                cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 26f, -10f), Quaternion.Euler(70f, 0f, 0f));
            }

            if (!FindAnyObjectByType<Light>())
            {
                GameObject lightObject = new("Wave Light");
                lightObject.transform.SetParent(transform, false);
                lightObject.AddComponent<Light>().type = LightType.Directional;
                lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
        }
    }
}
