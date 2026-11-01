using System.Collections;
using NUnit.Framework;
using Pretzel.Audio;
using UnityEngine;
using UnityEngine.TestTools;

namespace Pretzel.Sfx.PlayTests
{
    /// <summary>Plays through the real AudioService, which is the only source of valid voice handles.</summary>
    public sealed class SfxVoiceLimitTests
    {
        private GameObject _audioObject;
        private AudioClip _clip;
        private AudioCue _cue;
        private SfxLibrary _library;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _audioObject = new GameObject("Audio", typeof(AudioListener));
            _audioObject.AddComponent<AudioService>();
            _clip = AudioClip.Create("long", 44100 * 3, 1, 44100, false);
            _cue = ScriptableObject.CreateInstance<AudioCue>();
            _cue.SetClips(_clip);
            _library = ScriptableObject.CreateInstance<SfxLibrary>();
            _library.SetEntries(new[] { new SfxEntry("boom", _cue, 0f, 2) });
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_audioObject);
            Object.DestroyImmediate(_library);
            Object.DestroyImmediate(_cue);
            Object.DestroyImmediate(_clip);
        }

        [UnityTest]
        public IEnumerator TheVoiceLimit_SkipsPlaysWhileVoicesAreBusy()
        {
            AudioService service = _audioObject.GetComponent<AudioService>();
            SfxPlayer player = new(service, _library);

            AudioHandle first = player.Play("boom");
            AudioHandle second = player.Play("boom");
            AudioHandle third = player.Play("boom");
            yield return null;

            Assert.IsTrue(first.IsValid);
            Assert.IsTrue(second.IsValid);
            Assert.IsFalse(third.IsValid, "A third voice must be skipped while two still play.");
            Assert.AreEqual(2, player.Played);
            Assert.AreEqual(1, player.Suppressed);
        }

        [UnityTest]
        public IEnumerator AStoppedVoice_FreesASlot()
        {
            AudioService service = _audioObject.GetComponent<AudioService>();
            SfxPlayer player = new(service, _library);
            AudioHandle first = player.Play("boom");
            player.Play("boom");
            yield return null;

            service.Stop(first);
            yield return null;

            Assert.IsTrue(player.Play("boom").IsValid);
        }

        [UnityTest]
        public IEnumerator Stop_SilencesEveryVoiceOfASound()
        {
            AudioService service = _audioObject.GetComponent<AudioService>();
            SfxPlayer player = new(service, _library);
            AudioHandle first = player.Play("boom");
            AudioHandle second = player.Play("boom");
            yield return null;

            player.Stop("boom");
            yield return null;

            Assert.IsFalse(service.IsPlaying(first));
            Assert.IsFalse(service.IsPlaying(second));
        }
    }
}
