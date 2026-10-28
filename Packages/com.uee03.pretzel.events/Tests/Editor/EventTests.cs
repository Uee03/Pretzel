using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Pretzel.Events.Tests
{
    public sealed class EventTests
    {
        // Value type: covers struct messages, the recommended message shape.
        private readonly struct Scored
        {
            public readonly int Points;

            public Scored(int points) => Points = points;
        }

        [Test]
        public void BusDeliversByTypeAndUnsubscribes()
        {
            EventBus bus = new();
            int total = 0;
            IDisposable subscription = bus.Subscribe<Scored>(e => total += e.Points);
            bus.Subscribe<string>(_ => total += 1000);

            bus.Publish(new Scored(5));
            Assert.That(total, Is.EqualTo(5));
            Assert.That(bus.SubscriberCount<Scored>(), Is.EqualTo(1));

            subscription.Dispose();
            bus.Publish(new Scored(5));
            Assert.That(total, Is.EqualTo(5));
            Assert.That(bus.SubscriberCount<Scored>(), Is.EqualTo(0));
        }

        [Test]
        public void ChangesDuringPublishApplyNextTime()
        {
            EventBus bus = new();
            List<string> calls = new();
            IDisposable first = null;
            first = bus.Subscribe<int>(_ =>
            {
                calls.Add("first");
                first.Dispose();
                bus.Subscribe<int>(value => calls.Add("late"));
            });
            bus.Subscribe<int>(_ => calls.Add("second"));

            bus.Publish(1);
            Assert.That(calls, Is.EqualTo(new[] { "first", "second" }));
            calls.Clear();
            bus.Publish(2);
            Assert.That(calls, Is.EqualTo(new[] { "second", "late" }));
        }

        [Test]
        public void AFailingHandlerDoesNotStopTheOthers()
        {
            EventBus bus = new();
            bool reached = false;
            bus.Subscribe<int>(_ => throw new InvalidOperationException("boom"));
            bus.Subscribe<int>(_ => reached = true);
            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            bus.Publish(1);
            Assert.That(reached, Is.True);
        }

        [Test]
        public void ChannelsRaiseToSubscribers()
        {
            IntEventChannel channel = ScriptableObject.CreateInstance<IntEventChannel>();
            VoidEventChannel ping = ScriptableObject.CreateInstance<VoidEventChannel>();
            try
            {
                int received = 0;
                int pings = 0;
                IDisposable subscription = channel.Subscribe(v => received = v);
                ping.Subscribe(() => pings++);

                channel.Raise(42);
                ping.Raise();
                Assert.That(received, Is.EqualTo(42));
                Assert.That(pings, Is.EqualTo(1));

                subscription.Dispose();
                channel.Raise(7);
                Assert.That(received, Is.EqualTo(42));
                Assert.That(channel.SubscriberCount, Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(channel);
                UnityEngine.Object.DestroyImmediate(ping);
            }
        }
    }
}
