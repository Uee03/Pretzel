using System;
using NUnit.Framework;
using UnityEngine;

namespace Pretzel.Pooling.Tests
{
    public sealed class ComponentPoolTests
    {
        private GameObject _prefab;
        private GameObject _root;

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
        }

        [Test]
        public void ReusesReleasedInstances()
        {
            using ComponentPool<PoolableProbe> pool = new(_prefab.GetComponent<PoolableProbe>(), _root.transform);
            PoolableProbe first = pool.Get(new Vector3(1f, 2f, 3f), Quaternion.identity);
            Assert.That(first.gameObject.activeSelf, Is.True);
            Assert.That(first.transform.position, Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Assert.That(first.WasActiveWhenTaken, Is.True);

            pool.Release(first);
            Assert.That(first.gameObject.activeSelf, Is.False);
            Assert.That(first.Returned, Is.EqualTo(1));

            PoolableProbe second = pool.Get();
            Assert.That(second, Is.SameAs(first));
            Assert.That(second.Taken, Is.EqualTo(2));
            Assert.That(pool.CountAll, Is.EqualTo(1));
        }

        [Test]
        public void PrewarmCreatesIdleInstancesWithoutCallbacks()
        {
            using ComponentPool<PoolableProbe> pool = new(_prefab.GetComponent<PoolableProbe>(), _root.transform);
            pool.Prewarm(4);
            Assert.That(pool.CountInactive, Is.EqualTo(4));
            Assert.That(_root.transform.childCount, Is.EqualTo(4));
            foreach (Transform child in _root.transform)
            {
                PoolableProbe probe = child.GetComponent<PoolableProbe>();
                Assert.That(probe.Taken + probe.Returned, Is.EqualTo(0));
                Assert.That(child.gameObject.activeSelf, Is.False);
            }
        }

        [Test]
        public void PooledObjectReleasesItselfOnce()
        {
            using ComponentPool<PoolableProbe> pool = new(_prefab.GetComponent<PoolableProbe>(), _root.transform);
            PoolableProbe item = pool.Get();
            PooledObject pooled = item.GetComponent<PooledObject>();
            Assert.That(pooled.IsInUse, Is.True);

            pooled.Release();
            pooled.Release();
            Assert.That(pool.CountInactive, Is.EqualTo(1));
            Assert.That(item.Returned, Is.EqualTo(1));
            Assert.That(pooled.IsInUse, Is.False);
        }

        [Test]
        public void DoubleReleaseThrowsWithCollectionCheck()
        {
            using ComponentPool<PoolableProbe> pool = new(_prefab.GetComponent<PoolableProbe>(), _root.transform);
            PoolableProbe item = pool.Get();
            pool.Release(item);
            Assert.Throws<InvalidOperationException>(() => pool.Release(item));
        }

        [Test]
        public void DisposeDestroysIdleAndLaterReleasedInstances()
        {
            ComponentPool<PoolableProbe> pool = new(_prefab.GetComponent<PoolableProbe>(), _root.transform);
            PoolableProbe idle = pool.Get();
            PoolableProbe active = pool.Get();
            pool.Release(idle);
            pool.Dispose();
            Assert.That(idle == null, Is.True);
            pool.Release(active);
            Assert.That(active == null, Is.True);
            Assert.Throws<ObjectDisposedException>(() => pool.Get());
        }

        [Test]
        public void RegistryKeepsOnePoolPerPrefab()
        {
            using PoolRegistry registry = new(_root.transform);
            PoolableProbe prefab = _prefab.GetComponent<PoolableProbe>();
            PoolableProbe a = registry.Spawn(prefab, Vector3.zero, Quaternion.identity);
            PoolRegistry.Despawn(a.gameObject);
            PoolableProbe b = registry.Spawn(prefab);
            Assert.That(b, Is.SameAs(a));
            Assert.That(registry.GetPool(prefab).CountAll, Is.EqualTo(1));
        }
    }
}
