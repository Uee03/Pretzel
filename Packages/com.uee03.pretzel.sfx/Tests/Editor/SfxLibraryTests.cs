using NUnit.Framework;
using Pretzel.Audio;
using UnityEngine;

namespace Pretzel.Sfx.Tests
{
    public sealed class SfxLibraryTests
    {
        private SfxLibrary _library;
        private AudioCue _first;
        private AudioCue _second;

        [SetUp]
        public void SetUp()
        {
            _library = ScriptableObject.CreateInstance<SfxLibrary>();
            _first = ScriptableObject.CreateInstance<AudioCue>();
            _second = ScriptableObject.CreateInstance<AudioCue>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_library);
            Object.DestroyImmediate(_first);
            Object.DestroyImmediate(_second);
        }

        [Test]
        public void TryGet_FindsAnEntryById()
        {
            _library.SetEntries(new[] { new SfxEntry("hit", _first, 0.25f, 3) });

            Assert.IsTrue(_library.TryGet("hit", out SfxEntry entry));
            Assert.AreSame(_first, entry.Cue);
            Assert.AreEqual(0.25f, entry.CooldownSeconds);
            Assert.AreEqual(3, entry.MaxConcurrent);
        }

        [Test]
        public void TryGet_IsCaseSensitive()
        {
            _library.SetEntries(new[] { new SfxEntry("Hit", _first) });

            Assert.IsFalse(_library.TryGet("hit", out _));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("missing")]
        public void TryGet_ReturnsFalseForUnknownIds(string id)
        {
            _library.SetEntries(new[] { new SfxEntry("hit", _first) });

            Assert.IsFalse(_library.TryGet(id, out SfxEntry entry));
            Assert.IsNull(entry);
        }

        [Test]
        public void ADuplicateId_UsesTheFirstEntry()
        {
            _library.SetEntries(new[] { new SfxEntry("hit", _first), new SfxEntry("hit", _second) });

            _library.TryGet("hit", out SfxEntry entry);

            Assert.AreSame(_first, entry.Cue);
        }

        [Test]
        public void SetEntries_RebuildsTheIndex()
        {
            _library.SetEntries(new[] { new SfxEntry("a", _first) });
            _library.TryGet("a", out _);

            _library.SetEntries(new[] { new SfxEntry("b", _second) });

            Assert.IsFalse(_library.TryGet("a", out _));
            Assert.IsTrue(_library.TryGet("b", out _));
        }

        [Test]
        public void NegativeLimits_AreClamped()
        {
            SfxEntry entry = new("x", _first, -1f, -4);

            Assert.AreEqual(0f, entry.CooldownSeconds);
            Assert.AreEqual(0, entry.MaxConcurrent);
        }
    }
}
