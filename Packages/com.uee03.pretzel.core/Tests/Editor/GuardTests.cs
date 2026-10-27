using System;
using NUnit.Framework;
using UnityEngine;

namespace Pretzel.Core.Tests
{
    public sealed class GuardTests
    {
        [Test]
        public void NotNullReturnsValueAndThrowsWithParameterName()
        {
            string value = "x";
            Assert.That(Guard.NotNull(value, "value"), Is.SameAs(value));
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => Guard.NotNull<string>(null, "value"));
            Assert.That(ex.ParamName, Is.EqualTo("value"));
        }

        [Test]
        public void NotDestroyedRejectsDestroyedObjects()
        {
            GameObject gameObject = new("guard");
            Assert.That(Guard.NotDestroyed(gameObject, "gameObject"), Is.SameAs(gameObject));
            UnityEngine.Object.DestroyImmediate(gameObject);
            Assert.Throws<ArgumentNullException>(() => Guard.NotDestroyed(gameObject, "gameObject"));
        }

        [Test]
        public void NotNullOrEmptyChecksStringsAndCollections()
        {
            Assert.Throws<ArgumentException>(() => Guard.NotNullOrEmpty((string)null, "s"));
            Assert.Throws<ArgumentException>(() => Guard.NotNullOrEmpty(string.Empty, "s"));
            Assert.Throws<ArgumentException>(() => Guard.NotNullOrEmpty(Array.Empty<int>(), "c"));
            Assert.That(Guard.NotNullOrEmpty(new[] { 1 }, "c"), Is.EqualTo(new[] { 1 }));
        }

        [TestCase(0, true)]
        [TestCase(10, true)]
        [TestCase(-1, false)]
        [TestCase(11, false)]
        public void InRangeIsInclusive(int value, bool valid)
        {
            if (valid)
            {
                Assert.That(Guard.InRange(value, 0, 10, "v"), Is.EqualTo(value));
            }
            else
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => Guard.InRange(value, 0, 10, "v"));
            }
        }

        [Test]
        public void FloatInRangeRejectsNaN()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Guard.InRange(float.NaN, 0f, 1f, "v"));
        }
    }
}
