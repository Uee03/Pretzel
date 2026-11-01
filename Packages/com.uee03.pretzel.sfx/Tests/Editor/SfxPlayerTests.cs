using System;
using NUnit.Framework;
using Pretzel.Audio;
using UnityEngine;
using UnityEngine.TestTools;

namespace Pretzel.Sfx.Tests
{
    public sealed class SfxPlayerTests
    {
        private FakeAudioService _audio;
        private SfxLibrary _library;
        private AudioCue _hit;
        private AudioCue _empty;
        private AudioClip _clip;
        private float _now;

        [SetUp]
        public void SetUp()
        {
            _audio = new FakeAudioService();
            _clip = AudioClip.Create("clip", 100, 1, 44100, false);
            _hit = ScriptableObject.CreateInstance<AudioCue>();
            _hit.SetClips(_clip);
            _empty = ScriptableObject.CreateInstance<AudioCue>();

            _library = ScriptableObject.CreateInstance<SfxLibrary>();
            _library.SetEntries(new[]
            {
                new SfxEntry("hit", _hit, cooldownSeconds: 0.5f),
                new SfxEntry("coin", _hit),
                new SfxEntry("silent", _empty)
            });
            _now = 0f;
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_library);
            UnityEngine.Object.DestroyImmediate(_hit);
            UnityEngine.Object.DestroyImmediate(_empty);
            UnityEngine.Object.DestroyImmediate(_clip);
        }

        private SfxPlayer Player() => new(_audio, _library, () => _now);

        [Test]
        public void ALibrarySound_IsPlayedThroughTheAudioService()
        {
            SfxPlayer player = Player();

            player.Play("coin");

            Assert.AreEqual(1, _audio.Cues.Count);
            Assert.AreSame(_hit, _audio.Cues[0]);
            Assert.AreEqual(1, player.Played);
        }

        [Test]
        public void APositionalPlay_ReachesTheServiceWithThePosition()
        {
            Player().Play("coin", new Vector3(1f, 2f, 3f));

            Assert.AreEqual(new Vector3(1f, 2f, 3f), _audio.Positions[0]);
        }

        [Test]
        public void AnUnknownId_DoesNotPlay_AndWarnsOnlyOnce()
        {
            SfxPlayer player = Player();
            LogAssert.Expect(LogType.Warning, "[SFX] 'nope' is not in the library, or its cue has no clips. It will not play.");

            player.Play("nope");
            player.Play("nope");

            Assert.AreEqual(0, _audio.Cues.Count);
        }

        [Test]
        public void ACueWithoutClips_DoesNotPlay()
        {
            LogAssert.Expect(LogType.Warning, "[SFX] 'silent' is not in the library, or its cue has no clips. It will not play.");

            Player().Play("silent");

            Assert.AreEqual(0, _audio.Cues.Count);
        }

        [Test]
        public void ANullId_DoesNotThrow()
        {
            LogAssert.Expect(LogType.Warning, "[SFX] '' is not in the library, or its cue has no clips. It will not play.");

            Assert.DoesNotThrow(() => Player().Play(null));
        }

        [Test]
        public void TheCooldown_SuppressesRapidRepeatsAndCountsThem()
        {
            SfxPlayer player = Player();

            player.Play("hit");
            _now = 0.2f;
            player.Play("hit");
            _now = 0.6f;
            player.Play("hit");

            Assert.AreEqual(2, _audio.Cues.Count);
            Assert.AreEqual(2, player.Played);
            Assert.AreEqual(1, player.Suppressed);
        }

        [Test]
        public void ASoundWithoutACooldown_PlaysEveryTime()
        {
            SfxPlayer player = Player();

            player.Play("coin");
            player.Play("coin");
            player.Play("coin");

            Assert.AreEqual(3, _audio.Cues.Count);
        }

        [Test]
        public void Stop_OfAnUnknownOrNullId_DoesNothing()
        {
            SfxPlayer player = Player();

            Assert.DoesNotThrow(() => player.Stop("nope"));
            Assert.DoesNotThrow(() => player.Stop(null));
        }

        [Test]
        public void TheConstructor_RejectsNulls()
        {
            Assert.Throws<ArgumentNullException>(() => new SfxPlayer(null, _library));
            Assert.Throws<ArgumentNullException>(() => new SfxPlayer(_audio, null));
        }
    }
}
