using System.Collections.Generic;
using NUnit.Framework;
using Pretzel.Events;
using Pretzel.Integrations.R3;
using R3;
using UnityEngine;

namespace Pretzel.Integrations.Tests
{
    public sealed class R3EventsTests
    {
        private readonly List<Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        [Test]
        public void BusMessagesBecomeAStreamAndUnsubscribeOnDispose()
        {
            EventBus bus = new();
            List<int> received = new();

            System.IDisposable subscription = bus.AsObservable<int>().Subscribe(received.Add);
            bus.Publish(1);
            bus.Publish(2);
            Assert.That(bus.SubscriberCount<int>(), Is.EqualTo(1));

            subscription.Dispose();
            bus.Publish(3);

            Assert.That(received, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(bus.SubscriberCount<int>(), Is.EqualTo(0));
        }

        [Test]
        public void BusStreamsWorkWithR3Operators()
        {
            EventBus bus = new();
            List<string> received = new();

            using System.IDisposable subscription = bus.AsObservable<int>()
                .Where(value => value % 2 == 0)
                .Select(value => "even " + value)
                .Subscribe(received.Add);
            for (int i = 1; i <= 5; i++)
            {
                bus.Publish(i);
            }

            Assert.That(received, Is.EqualTo(new[] { "even 2", "even 4" }));
        }

        [Test]
        public void AStreamCanPublishOnTheBus()
        {
            EventBus bus = new();
            Subject<int> source = new();
            List<int> received = new();
            using System.IDisposable listener = bus.Subscribe<int>(received.Add);

            System.IDisposable bridge = source.PublishTo(bus);
            source.OnNext(7);
            bridge.Dispose();
            source.OnNext(8);

            Assert.That(received, Is.EqualTo(new[] { 7 }));
            source.Dispose();
        }

        [Test]
        public void ChannelRaisesBecomeAStreamAndUnsubscribeOnDispose()
        {
            IntEventChannel channel = Create<IntEventChannel>();
            List<int> received = new();

            System.IDisposable subscription = channel.AsObservable().Subscribe(received.Add);
            channel.Raise(5);
            Assert.That(channel.SubscriberCount, Is.EqualTo(1));

            subscription.Dispose();
            channel.Raise(6);

            Assert.That(received, Is.EqualTo(new[] { 5 }));
            Assert.That(channel.SubscriberCount, Is.EqualTo(0));
        }

        [Test]
        public void VoidChannelEmitsUnitAndCanBeRaisedByAStream()
        {
            VoidEventChannel channel = Create<VoidEventChannel>();
            int emitted = 0;
            int raised = 0;
            using System.IDisposable stream = channel.AsObservable().Subscribe(_ => emitted++);
            using System.IDisposable listener = channel.Subscribe(() => raised++);

            channel.Raise();
            Subject<Unit> trigger = new();
            using System.IDisposable bridge = trigger.RaiseOn(channel);
            trigger.OnNext(Unit.Default);

            Assert.That(emitted, Is.EqualTo(2));
            Assert.That(raised, Is.EqualTo(2));
            trigger.Dispose();
        }

        [Test]
        public void AStreamCanRaiseAPayloadChannel()
        {
            IntEventChannel channel = Create<IntEventChannel>();
            Subject<int> source = new();
            List<int> received = new();
            using System.IDisposable listener = channel.Subscribe(received.Add);

            using System.IDisposable bridge = source.RaiseOn(channel);
            source.OnNext(42);

            Assert.That(received, Is.EqualTo(new[] { 42 }));
            source.Dispose();
        }

        private T Create<T>() where T : ScriptableObject
        {
            T instance = ScriptableObject.CreateInstance<T>();
            _created.Add(instance);
            return instance;
        }
    }
}
