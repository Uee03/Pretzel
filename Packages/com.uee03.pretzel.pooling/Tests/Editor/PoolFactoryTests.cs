using System;
using NUnit.Framework;
using UnityEngine;

namespace Pretzel.Pooling.Tests
{
    public sealed class PoolFactoryTests
    {
        private enum Shot
        {
            Bullet,
            Rocket
        }

        private GameObject _bullet;
        private GameObject _rocket;
        private PoolFactory<Shot, PoolableProbe> _factory;

        [SetUp]
        public void SetUp()
        {
            _bullet = new GameObject("bullet");
            _bullet.AddComponent<PoolableProbe>();
            _rocket = new GameObject("rocket");
            _rocket.AddComponent<PoolableProbe>();
            _factory = new PoolFactory<Shot, PoolableProbe>();
        }

        [TearDown]
        public void TearDown()
        {
            _factory.Dispose();
            UnityEngine.Object.DestroyImmediate(_bullet);
            UnityEngine.Object.DestroyImmediate(_rocket);
        }

        [Test]
        public void SpawnsByKeyAndGroupsIdleInstances()
        {
            _factory.Entries.Add(new PoolFactoryEntry<Shot, PoolableProbe>(Shot.Bullet, _bullet.GetComponent<PoolableProbe>(), 3));
            _factory.Entries.Add(new PoolFactoryEntry<Shot, PoolableProbe>(Shot.Rocket, _rocket.GetComponent<PoolableProbe>()));
            _factory.Initialize();

            Assert.That(_factory.GetPool(Shot.Bullet).CountInactive, Is.EqualTo(3));
            Assert.That(_factory.Keys, Is.EquivalentTo(new[] { Shot.Bullet, Shot.Rocket }));

            PoolableProbe rocket = _factory.Spawn(Shot.Rocket, Vector3.up, Quaternion.identity);
            Assert.That(rocket.name, Does.StartWith("rocket"));
            Assert.That(rocket.transform.parent.name, Is.EqualTo("[Rocket]"));
            Assert.That(rocket.transform.parent.parent.name, Is.EqualTo("[Pools]"));

            _factory.Release(rocket);
            Assert.That(_factory.GetPool(Shot.Rocket).CountInactive, Is.EqualTo(1));
        }

        [Test]
        public void InvalidEntriesFailFast()
        {
            _factory.Entries.Add(new PoolFactoryEntry<Shot, PoolableProbe>(Shot.Bullet, null));
            InvalidOperationException missing = Assert.Throws<InvalidOperationException>(() => _factory.Initialize());
            Assert.That(missing.Message, Does.Contain("entry 0"));

            _factory.Entries.Clear();
            _factory.Entries.Add(new PoolFactoryEntry<Shot, PoolableProbe>(Shot.Bullet, _bullet.GetComponent<PoolableProbe>()));
            _factory.Entries.Add(new PoolFactoryEntry<Shot, PoolableProbe>(Shot.Bullet, _rocket.GetComponent<PoolableProbe>()));
            Assert.Throws<InvalidOperationException>(() => _factory.Initialize());
            Assert.That(_factory.IsInitialized, Is.False);
        }

        [Test]
        public void UnknownKeysAndUninitializedUseAreReported()
        {
            Assert.Throws<InvalidOperationException>(() => _factory.Spawn(Shot.Bullet));
            _factory.Initialize();
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => _factory.Spawn(Shot.Rocket));
            Assert.That(_factory.TrySpawn(Shot.Rocket, Vector3.zero, Quaternion.identity, out PoolableProbe none), Is.False);
            Assert.That(none, Is.Null);

            _factory.Register(Shot.Rocket, _rocket.GetComponent<PoolableProbe>());
            Assert.That(_factory.TrySpawn(Shot.Rocket, Vector3.zero, Quaternion.identity, out PoolableProbe rocket), Is.True);
            Assert.That(rocket, Is.Not.Null);
        }

        [Test]
        public void DisposeRemovesTheRoot()
        {
            _factory.Entries.Add(new PoolFactoryEntry<Shot, PoolableProbe>(Shot.Bullet, _bullet.GetComponent<PoolableProbe>(), 2));
            _factory.Initialize();
            Transform root = _factory.GetPool(Shot.Bullet).Get().transform.parent.parent;
            _factory.Dispose();
            Assert.That(root == null, Is.True);
            Assert.That(_factory.IsInitialized, Is.False);
        }
    }
}
