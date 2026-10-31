using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Pretzel.Pooling.Tests
{
    public sealed class PoolInterfaceTests
    {
        private enum Shot
        {
            Bullet
        }

        private GameObject _prefab;
        private GameObject _root;
        private readonly List<GameObject> _created = new();

        [SetUp]
        public void SetUp()
        {
            _prefab = new GameObject("pool-prefab");
            _prefab.AddComponent<PoolableProbe>();
            _root = new GameObject("pool-root");
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_prefab);
            UnityEngine.Object.DestroyImmediate(_root);
            foreach (GameObject created in _created)
            {
                UnityEngine.Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        [Test]
        public void ComponentPoolWorksThroughTheInterface()
        {
            using IComponentPool<PoolableProbe> pool = new ComponentPool<PoolableProbe>(_prefab.GetComponent<PoolableProbe>(), _root.transform);
            pool.Prewarm(2);
            Assert.That(pool.CountInactive, Is.EqualTo(2));

            PoolableProbe probe = pool.Get(Vector3.one, Quaternion.identity);
            Assert.That(pool.CountActive, Is.EqualTo(1));
            Assert.That(pool.Prefab, Is.SameAs(_prefab.GetComponent<PoolableProbe>()));

            pool.Release(probe);
            Assert.That(pool.CountInactive, Is.EqualTo(2));
            pool.Clear();
            Assert.That(pool.CountAll, Is.EqualTo(0));
        }

        [Test]
        public void RegistryHandsOutTheSamePoolThroughTheInterface()
        {
            using PoolRegistry concrete = new(_root.transform);
            IPoolRegistry registry = concrete;
            PoolableProbe prefab = _prefab.GetComponent<PoolableProbe>();

            Assert.That(registry.GetPool(prefab), Is.SameAs(concrete.GetPool(prefab)));
            PoolableProbe spawned = registry.Spawn(prefab, Vector3.zero, Quaternion.identity);
            Assert.That(registry.GetPool(prefab).CountActive, Is.EqualTo(1));
            PoolRegistry.Despawn(spawned.gameObject);
            Assert.That(registry.GetPool(prefab).CountActive, Is.EqualTo(0));
        }

        [Test]
        public void FactorySpawnsThroughTheInterface()
        {
            PoolFactory<Shot, PoolableProbe> concrete = new();
            concrete.AddEntry(Shot.Bullet, _prefab.GetComponent<PoolableProbe>(), 1);
            IPoolFactory<Shot, PoolableProbe> factory = concrete;
            try
            {
                factory.Initialize();
                Assert.That(factory.Contains(Shot.Bullet), Is.True);
                Assert.That(factory.GetPool(Shot.Bullet).CountInactive, Is.EqualTo(1));

                Assert.That(factory.TrySpawn(Shot.Bullet, Vector3.zero, Quaternion.identity, out PoolableProbe shot), Is.True);
                factory.Release(shot);
                Assert.That(factory.GetPool(Shot.Bullet).CountActive, Is.EqualTo(0));
            }
            finally
            {
                factory.Dispose();
            }
        }

        [Test]
        public void SpawnerRunsAgainstAFakePool()
        {
            FakePool fake = new(CreateProbe());
            Shooter shooter = new(fake);

            shooter.Fire();
            shooter.Fire();

            Assert.That(fake.GetCount, Is.EqualTo(2));
            Assert.That(fake.CountActive, Is.EqualTo(2));
        }

        private PoolableProbe CreateProbe()
        {
            GameObject go = new("fake-probe");
            _created.Add(go);
            return go.AddComponent<PoolableProbe>();
        }

        // The consumer takes the interface, so the test needs no prefab instantiation.
        private sealed class Shooter
        {
            private readonly IComponentPool<PoolableProbe> _pool;

            public Shooter(IComponentPool<PoolableProbe> pool)
            {
                _pool = pool;
            }

            public void Fire() => _pool.Get(Vector3.zero, Quaternion.identity);
        }

        private sealed class FakePool : IComponentPool<PoolableProbe>
        {
            private readonly PoolableProbe _instance;

            public FakePool(PoolableProbe instance)
            {
                _instance = instance;
            }

            public int GetCount { get; private set; }

            public int CountActive => GetCount;

            public int CountInactive => 0;

            public int CountAll => GetCount;

            public PoolableProbe Prefab => _instance;

            public PoolableProbe Get()
            {
                GetCount++;
                return _instance;
            }

            public PoolableProbe Get(Transform parent) => Get();

            public PoolableProbe Get(Vector3 position, Quaternion rotation, Transform parent = null) => Get();

            public void Release(PoolableProbe item)
            {
            }

            public void Prewarm(int count)
            {
            }

            public void Clear()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
