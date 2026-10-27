using System.Collections.Generic;
using NUnit.Framework;

namespace Pretzel.Extensions.Tests
{
    public sealed class ExtensionsTests
    {
        [TestCase("Hello, World!", "HelloWorld")]
        [TestCase("a b-c", "abc")]
        [TestCase("", "")]
        public void ToAlphanumericKeepsLettersAndDigits(string input, string expected)
        {
            Assert.That(input.ToAlphanumeric(), Is.EqualTo(expected));
        }

        [Test]
        public void InnerTrimCollapsesWhiteSpace()
        {
            Assert.That("  a   b \t c ".InnerTrim(), Is.EqualTo("a b c"));
        }

        [TestCase("player@example.com", true)]
        [TestCase("player@example", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void IsValidEmail(string input, bool expected)
        {
            Assert.That(input.IsValidEmail(), Is.EqualTo(expected));
        }

        [TestCase("https://example.com/path", true)]
        [TestCase("ftp://example.com", false)]
        [TestCase("not a url", false)]
        public void IsValidUrlAcceptsHttpOnly(string input, bool expected)
        {
            Assert.That(input.IsValidUrl(), Is.EqualTo(expected));
        }

        [Test]
        public void Base64RoundTrips()
        {
            const string text = "Pretzel \u00e9\u00e8";
            Assert.That(text.ToBase64().FromBase64(), Is.EqualTo(text));
            Assert.That(((string)null).ToBase64(), Is.Empty);
        }

        [TestCase(0f, "00:00")]
        [TestCase(65f, "01:05")]
        [TestCase(3725f, "62:05")]
        [TestCase(-5f, "00:00")]
        [TestCase(float.NaN, "00:00")]
        public void ToTimeMS(float seconds, string expected)
        {
            Assert.That(seconds.ToTimeMS(), Is.EqualTo(expected));
        }

        [Test]
        public void ToTimeHMSKeepsCountingHours()
        {
            Assert.That(90000f.ToTimeHMS(), Is.EqualTo("25:00:00"));
        }

        [Test]
        public void ShuffleKeepsTheSameItems()
        {
            List<int> list = new() { 1, 2, 3, 4, 5, 6, 7, 8 };
            list.Shuffle();
            Assert.That(list, Is.EquivalentTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
        }

        [Test]
        public void SwapAndRandomItem()
        {
            List<string> list = new() { "a", "b" };
            list.Swap(0, 1);
            Assert.That(list, Is.EqualTo(new[] { "b", "a" }));
            Assert.That(list.TryGetRandomItem(out string item), Is.True);
            Assert.That(list, Does.Contain(item));
            Assert.That(new List<string>().TryGetRandomItem(out _), Is.False);
        }
    }
}
