using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Pretzel.Integrations.VContainer.Tests
{
    public sealed class PretzelLifetimeScopeTests
    {
        private readonly List<Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                if (created != null)
                {
                    Object.DestroyImmediate(created);
                }
            }

            _created.Clear();
        }

        [UnityTest]
        public IEnumerator SettingsInstallersAndOwnMonoInstallers_AreInstalled()
        {
            TestSettings settings = Track(ScriptableObject.CreateInstance<TestSettings>());
            CounterInstaller installer = Track(ScriptableObject.CreateInstance<CounterInstaller>());
            GameObject root = NewInactive("Root");
            PretzelLifetimeScope scope = root.AddComponent<PretzelLifetimeScope>();
            SetList(scope, "_settings", new List<ScriptableObject> { settings });
            SetList(scope, "_installers", new List<ScriptableInstaller> { installer });
            new GameObject("Installer", typeof(GreeterInstaller)).transform.SetParent(root.transform);

            root.SetActive(true);
            yield return null;

            Assert.That(scope.Container.Resolve<TestSettings>(), Is.SameAs(settings));
            Assert.That(scope.Container.Resolve<ICounter>().Settings, Is.SameAs(settings));
            Assert.That(scope.Container.Resolve<Greeter>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator MonoInstallersOfANestedScope_StayInThatScope()
        {
            GameObject root = NewInactive("Root");
            PretzelLifetimeScope parent = root.AddComponent<PretzelLifetimeScope>();
            GameObject nested = new("Nested");
            nested.SetActive(false);
            nested.transform.SetParent(root.transform);
            nested.AddComponent<GreeterInstaller>();
            PretzelLifetimeScope child = nested.AddComponent<PretzelLifetimeScope>();
            child.parentReference.Object = parent;

            root.SetActive(true);
            yield return null;
            nested.SetActive(true);
            yield return null;

            Assert.That(parent.Container.TryResolve(out Greeter _), Is.False);
            Assert.That(child.Container.Resolve<Greeter>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator ComponentInHierarchy_ResolvesInADontDestroyOnLoadScope()
        {
            GameObject root = NewInactive("Root");
            Object.DontDestroyOnLoad(root);
            PretzelLifetimeScope scope = root.AddComponent<PretzelLifetimeScope>();
            SetList(scope, "_installers", new List<ScriptableInstaller> { Track(ScriptableObject.CreateInstance<ProbeInstaller>()) });
            new GameObject("Probe", typeof(Probe)).transform.SetParent(root.transform);

            root.SetActive(true);
            yield return null;

            Assert.That(scope.Container.Resolve<Probe>(), Is.SameAs(root.GetComponentInChildren<Probe>()));
        }

        [Test]
        public void EmptyInstallerSlot_FailsWithTheSlotName()
        {
            GameObject root = NewInactive("Root");
            PretzelLifetimeScope scope = root.AddComponent<PretzelLifetimeScope>();
            scope.autoRun = false;
            SetList(scope, "_installers", new List<ScriptableInstaller> { null });
            root.SetActive(true);

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => scope.Build());
            Assert.That(error.Message, Does.Contain("Installers element 0"));
        }

        private GameObject NewInactive(string name)
        {
            GameObject gameObject = new(name);
            gameObject.SetActive(false);
            return Track(gameObject);
        }

        private T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }

        private static void SetList<T>(PretzelLifetimeScope scope, string field, List<T> value)
        {
            typeof(PretzelLifetimeScope).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(scope, value);
        }
    }
}
