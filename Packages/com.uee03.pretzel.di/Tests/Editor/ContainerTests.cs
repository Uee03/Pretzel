using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Pretzel.DI.Tests
{
    public sealed class ContainerTests
    {
        public interface IClock
        {
        }

        public interface IScore
        {
            int Value { get; }
        }

        public sealed class Clock : IClock, IDisposable
        {
            public bool Disposed { get; private set; }

            public void Dispose() => Disposed = true;
        }

        public sealed class Score : IScore
        {
            public Score(IClock clock)
            {
                Clock = clock;
            }

            public IClock Clock { get; }

            public int Value => 3;
        }

        public sealed class Optional
        {
            public Optional(int bonus = 5)
            {
                Bonus = bonus;
            }

            public int Bonus { get; }
        }

        public sealed class Chooser
        {
            public Chooser()
            {
            }

            [Inject]
            public Chooser(IClock clock)
            {
                UsedInject = clock != null;
            }

            public Chooser(IClock clock, IScore score)
            {
            }

            public bool UsedInject { get; }
        }

        public class Target
        {
            [Inject] private IClock _clock = null;

            public IClock Clock => _clock;

            [Inject] public IScore Score { get; private set; }

            public IClock FromMethod { get; private set; }

            [Inject]
            private void Construct(IClock clock) => FromMethod = clock;
        }

        public sealed class DerivedTarget : Target
        {
            [Inject] public IObjectResolver Resolver;
        }

        public sealed class LoopA
        {
            public LoopA(LoopB b)
            {
            }
        }

        public sealed class LoopB
        {
            public LoopB(LoopA a)
            {
            }
        }

        public sealed class Entry : IStartable, ITickable
        {
            public int Starts;
            public int Ticks;

            public void Start() => Starts++;

            public void Tick() => Ticks++;
        }

        [Test]
        public void LifetimesControlInstanceSharing()
        {
            ContainerBuilder builder = new();
            builder.Register<IClock, Clock>(Lifetime.Singleton);
            builder.Register<IScore, Score>(Lifetime.Transient);
            builder.Register<Optional>(Lifetime.Scoped);
            Container root = builder.Build();
            Container scope = root.CreateScope();

            Assert.That(root.Resolve<IClock>(), Is.SameAs(scope.Resolve<IClock>()));
            Assert.That(root.Resolve<IScore>(), Is.Not.SameAs(root.Resolve<IScore>()));
            Assert.That(root.Resolve<Optional>(), Is.SameAs(root.Resolve<Optional>()));
            Assert.That(root.Resolve<Optional>(), Is.Not.SameAs(scope.Resolve<Optional>()));
            Assert.That(((Score)root.Resolve<IScore>()).Clock, Is.SameAs(root.Resolve<IClock>()));
        }

        [Test]
        public void DefaultParameterValuesFillMissingServices()
        {
            ContainerBuilder builder = new();
            builder.Register<Optional>(Lifetime.Transient);
            Assert.That(builder.Build().Resolve<Optional>().Bonus, Is.EqualTo(5));
        }

        [Test]
        public void InjectConstructorWinsOverLongestConstructor()
        {
            ContainerBuilder builder = new();
            builder.Register<IClock, Clock>(Lifetime.Singleton);
            builder.Register<IScore, Score>(Lifetime.Singleton);
            builder.Register<Chooser>(Lifetime.Transient);
            Assert.That(builder.Build().Resolve<Chooser>().UsedInject, Is.True);
        }

        [Test]
        public void InjectFillsFieldsPropertiesAndMethodsIncludingBaseClasses()
        {
            ContainerBuilder builder = new();
            builder.Register<IClock, Clock>(Lifetime.Singleton);
            builder.Register<IScore, Score>(Lifetime.Singleton);
            Container container = builder.Build();
            DerivedTarget target = new();
            container.Inject(target);

            Assert.That(target.Clock, Is.SameAs(container.Resolve<IClock>()));
            Assert.That(target.FromMethod, Is.SameAs(target.Clock));
            Assert.That(target.Score, Is.Not.Null);
            Assert.That(target.Resolver, Is.SameAs(container));
        }

        [Test]
        public void AsImplementedInterfacesSharesOneSingleton()
        {
            ContainerBuilder builder = new();
            builder.Register<Clock>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            Container container = builder.Build();
            Assert.That(container.Resolve<IClock>(), Is.SameAs(container.Resolve<Clock>()));
            Assert.That(container.IsRegistered(typeof(IDisposable)), Is.False);
        }

        [Test]
        public void InstancesAndFactories()
        {
            Clock clock = new();
            ContainerBuilder builder = new();
            builder.RegisterInstance<IClock>(clock);
            builder.RegisterFactory<IScore>(r => new Score(r.Resolve<IClock>()), Lifetime.Singleton);
            Container container = builder.Build();
            Assert.That(container.Resolve<IClock>(), Is.SameAs(clock));
            Assert.That(((Score)container.Resolve<IScore>()).Clock, Is.SameAs(clock));

            container.Dispose();
            Assert.That(clock.Disposed, Is.False, "A registered instance belongs to its creator.");
        }

        [Test]
        public void BuildValidatesDependenciesAndDuplicates()
        {
            ContainerBuilder missing = new();
            missing.Register<IScore, Score>(Lifetime.Transient);
            Assert.That(() => missing.Build(), Throws.TypeOf<ContainerException>().With.Message.Contains("IClock"));

            ContainerBuilder duplicate = new();
            duplicate.Register<IClock, Clock>(Lifetime.Singleton);
            duplicate.Register<IClock, Clock>(Lifetime.Transient);
            Assert.Throws<ContainerException>(() => duplicate.Build());

            ContainerBuilder unity = new();
            unity.Register<GameObject>(Lifetime.Transient);
            Assert.Throws<ContainerException>(() => unity.Build());
        }

        [Test]
        public void CircularDependenciesAreReported()
        {
            ContainerBuilder builder = new();
            builder.Register<LoopA>(Lifetime.Transient);
            builder.Register<LoopB>(Lifetime.Transient);
            Container container = builder.Build();
            Assert.That(() => container.Resolve<LoopA>(), Throws.TypeOf<ContainerException>().With.Message.Contains("Circular"));
        }

        [Test]
        public void ChildScopesSeeParentAndCanOverride()
        {
            ContainerBuilder builder = new();
            builder.Register<IClock, Clock>(Lifetime.Singleton);
            Container root = builder.Build();
            Clock replacement = new();
            Container child = root.CreateScope(b => b.RegisterInstance<IClock>(replacement));
            Container grandChild = child.CreateScope(b => b.Register<IScore, Score>(Lifetime.Transient));

            Assert.That(child.Resolve<IClock>(), Is.SameAs(replacement));
            Assert.That(((Score)grandChild.Resolve<IScore>()).Clock, Is.SameAs(replacement));
            Assert.That(root.IsRegistered(typeof(IScore)), Is.False);
        }

        [Test]
        public void DisposeCascadesToCreatedInstancesAndChildren()
        {
            ContainerBuilder builder = new();
            builder.Register<IClock, Clock>(Lifetime.Singleton);
            Container root = builder.Build();
            Container child = root.CreateScope(b => b.Register<Clock>(Lifetime.Scoped));
            Clock rootClock = (Clock)root.Resolve<IClock>();
            Clock childClock = child.Resolve<Clock>();

            root.Dispose();
            Assert.That(rootClock.Disposed, Is.True);
            Assert.That(childClock.Disposed, Is.True);
            Assert.That(child.IsDisposed, Is.True);
            Assert.Throws<ObjectDisposedException>(() => root.Resolve<IClock>());
        }

        [Test]
        public void EntryPointsStartOnceAndTick()
        {
            ContainerBuilder builder = new();
            builder.RegisterEntryPoint<Entry>();
            Container container = builder.Build();
            IReadOnlyList<object> entryPoints = container.ResolveEntryPoints();
            EntryPointDispatcher dispatcher = new(entryPoints);
            dispatcher.Start();
            dispatcher.Start();
            dispatcher.Tick();
            dispatcher.Tick();

            Entry entry = container.Resolve<Entry>();
            Assert.That(entry, Is.SameAs(entryPoints[0]));
            Assert.That(entry.Starts, Is.EqualTo(1));
            Assert.That(entry.Ticks, Is.EqualTo(2));
        }

        [Test]
        public void InjectGameObjectFillsComponents()
        {
            ContainerBuilder builder = new();
            builder.Register<IClock, Clock>(Lifetime.Singleton);
            Container container = builder.Build();
            GameObject parent = new("di-parent");
            GameObject child = new("di-child");
            child.transform.SetParent(parent.transform);
            child.SetActive(false);
            InjectedBehaviour behaviour = child.AddComponent<InjectedBehaviour>();
            try
            {
                container.InjectGameObject(parent);
                Assert.That(behaviour.Resolver, Is.SameAs(container), "Inactive children are injected too.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }
    }
}
