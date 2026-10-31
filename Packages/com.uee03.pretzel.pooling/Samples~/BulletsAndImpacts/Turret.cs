using System.Collections.Generic;
using Pretzel.Pooling;
using UnityEngine;

namespace Pretzel.Samples.Pooling.Bullets
{
    /// <summary>
    /// The composition root of the sample: builds a ground and two templates in code, owns the pools, and fires bullets at random
    /// ground points. The bullets spawn impact effects through <see cref="IEffectSpawner"/>, so you see one pool feeding another.
    /// In a project, replace the templates with prefabs.
    /// </summary>
    public sealed class Turret : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _shotsPerSecond = 25f;
        [SerializeField, Min(1)] private int _maxBullets = 100;
        [SerializeField, Min(1f)] private float _targetRadius = 9f;

        private readonly List<Material> _materials = new();
        private IPoolRegistry _registry;
        private PooledEffectSpawner _effects;
        private Bullet _bulletTemplate;
        private float _nextShot;
        private string _text = string.Empty;
        private float _nextRefresh;
        private GUIStyle _style;

        private void Awake()
        {
            BuildWorld();
            Transform templates = CreateInactiveHolder();
            _bulletTemplate = CreateBulletTemplate(templates);
            ImpactFlash impactTemplate = CreateImpactTemplate(templates);

            _registry = new PoolRegistry(transform, defaultCapacity: 32, maxSize: 256);
            _registry.Prewarm(_bulletTemplate, 40);
            _effects = new PooledEffectSpawner(_registry, impactTemplate, 40);
        }

        private void Update()
        {
            if (Time.time >= _nextShot)
            {
                _nextShot = Time.time + 1f / _shotsPerSecond;
                Fire();
            }

            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + 0.25f;
                IComponentPool<Bullet> bullets = _registry.GetPool(_bulletTemplate);
                IComponentPool<ImpactFlash> impacts = _effects.Pool;
                _text = $"Bullets: {bullets.CountActive} active, {bullets.CountInactive} idle, {bullets.CountAll} created\n" +
                        $"Impacts: {impacts.CountActive} active, {impacts.CountInactive} idle, {impacts.CountAll} created";
            }
        }

        private void OnDestroy()
        {
            _effects?.Dispose();
            foreach (Material material in _materials)
            {
                Destroy(material);
            }
        }

        private void OnGUI()
        {
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 15, normal = { textColor = Color.white } };
            GUI.Label(new Rect(12f, 8f, 700f, 60f), _text, _style);
        }

        private void Fire()
        {
            IComponentPool<Bullet> pool = _registry.GetPool(_bulletTemplate);
            if (pool.CountActive >= _maxBullets)
            {
                return;
            }

            Vector2 offset = Random.insideUnitCircle * _targetRadius;
            Vector3 target = new(offset.x, 0f, offset.y);
            Vector3 muzzle = transform.position;
            Bullet bullet = pool.Get(muzzle, Quaternion.LookRotation(target - muzzle));
            bullet.Launch(_effects);
        }

        private Transform CreateInactiveHolder()
        {
            Transform holder = new GameObject("[Templates]").transform;
            holder.SetParent(transform, false);
            holder.gameObject.SetActive(false);
            return holder;
        }

        private Bullet CreateBulletTemplate(Transform parent)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Bullet";
            body.transform.SetParent(parent, false);
            body.transform.localScale = Vector3.one * 0.25f;
            Destroy(body.GetComponent<Collider>());
            Tint(body, new Color(1f, 0.85f, 0.2f));
            return body.AddComponent<Bullet>();
        }

        private ImpactFlash CreateImpactTemplate(Transform parent)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Impact";
            body.transform.SetParent(parent, false);
            Destroy(body.GetComponent<Collider>());
            Tint(body, new Color(1f, 0.4f, 0.1f));
            return body.AddComponent<ImpactFlash>();
        }

        private void BuildWorld()
        {
            // The turret sits above and behind the target area.
            transform.position = new Vector3(0f, 4f, -12f);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(transform, false);
            ground.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
            Tint(ground, new Color(0.25f, 0.28f, 0.32f));

            if (!Camera.main)
            {
                GameObject cameraObject = new("Turret Camera") { tag = "MainCamera" };
                cameraObject.transform.SetParent(transform, false);
                cameraObject.AddComponent<Camera>();
                cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 14f, -16f), Quaternion.Euler(45f, 0f, 0f));
            }

            if (!FindAnyObjectByType<Light>())
            {
                GameObject lightObject = new("Turret Light");
                lightObject.transform.SetParent(transform, false);
                lightObject.AddComponent<Light>().type = LightType.Directional;
                lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
        }

        private void Tint(GameObject target, Color color)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            Material material = new(renderer.sharedMaterial) { color = color };
            renderer.sharedMaterial = material;
            _materials.Add(material);
        }
    }
}
