using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Pretzel.Core.Tests
{
    public sealed class CompositeDisposableTests
    {
        [Test]
        public void DisposesEveryItemInReverseOrder()
        {
            string order = string.Empty;
            CompositeDisposable bag = new();
            bag.Add(new DisposableAction(() => order += "a"));
            bag.Add(new DisposableAction(() => order += "b"));
            bag.Add(new DisposableAction(() => order += "c"));

            bag.Dispose();

            Assert.That(order, Is.EqualTo("cba"));
            Assert.That(bag.Count, Is.EqualTo(0));
            Assert.That(bag.IsDisposed, Is.True);
        }

        [Test]
        public void DisposingTwiceDoesNotRunItemsTwice()
        {
            int calls = 0;
            CompositeDisposable bag = new();
            bag.Add(new DisposableAction(() => calls++));

            bag.Dispose();
            bag.Dispose();

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void ClearKeepsTheBagUsable()
        {
            int calls = 0;
            CompositeDisposable bag = new();
            bag.Add(new DisposableAction(() => calls++));

            bag.Clear();
            bag.Add(new DisposableAction(() => calls++));
            bag.Dispose();

            Assert.That(calls, Is.EqualTo(2));
            Assert.That(bag.IsDisposed, Is.True);
        }

        [Test]
        public void AnItemAddedAfterDisposeIsDisposedAtOnce()
        {
            int calls = 0;
            CompositeDisposable bag = new();
            bag.Dispose();

            bag.Add(new DisposableAction(() => calls++));

            Assert.That(calls, Is.EqualTo(1));
            Assert.That(bag.Count, Is.EqualTo(0));
        }

        [Test]
        public void AFailingItemIsLoggedAndTheOthersStillRun()
        {
            int calls = 0;
            CompositeDisposable bag = new();
            bag.Add(new DisposableAction(() => calls++));
            bag.Add(new DisposableAction(() => throw new InvalidOperationException("boom")));
            bag.Add(new DisposableAction(() => calls++));
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("boom"));

            bag.Dispose();

            Assert.That(calls, Is.EqualTo(2));
        }

        [Test]
        public void AddToReturnsTheSameItem()
        {
            CompositeDisposable bag = new();
            DisposableAction action = new(() => { });

            Assert.That(action.AddTo(bag), Is.SameAs(action));
            Assert.That(bag.Count, Is.EqualTo(1));
        }

        [Test]
        public void NullItemsAreRejected()
        {
            CompositeDisposable bag = new();

            Assert.Throws<ArgumentNullException>(() => bag.Add<IDisposable>(null));
        }
    }
}
