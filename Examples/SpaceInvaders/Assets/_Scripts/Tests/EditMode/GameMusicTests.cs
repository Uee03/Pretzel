using NUnit.Framework;
using Pretzel.Events;
using SpaceInvaders.Audio;
using SpaceInvaders.Gameplay;
using UnityEngine;

namespace SpaceInvaders.Tests
{
    public sealed class GameMusicTests
    {
        private EventBus _bus;
        private FakeAudio _audio;
        private SoundBank _bank;
        private AudioClip _title;
        private AudioClip _gameplay;
        private AudioClip _lastStand;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
            _audio = new FakeAudio();
            _title = AudioClip.Create("title", 100, 1, 44100, false);
            _gameplay = AudioClip.Create("gameplay", 100, 1, 44100, false);
            _lastStand = AudioClip.Create("lastStand", 100, 1, 44100, false);
            _bank = ScriptableObject.CreateInstance<SoundBank>();
            Set("_titleMusic", _title);
            Set("_gameplayMusic", _gameplay);
            Set("_lastStandMusic", _lastStand);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_bank);
            Object.DestroyImmediate(_title);
            Object.DestroyImmediate(_gameplay);
            Object.DestroyImmediate(_lastStand);
        }

        [Test]
        public void TheFirstWave_PlaysTheGameplayLoopAtOnce()
        {
            using GameMusic music = Start();

            CollectionAssert.AreEqual(new[] { _gameplay }, _audio.Music);
        }

        [Test]
        public void FromWaveFive_TheLastStandPlays()
        {
            using GameMusic music = Start();

            _bus.Publish(new WaveStarted(4, "Four"));
            _bus.Publish(new WaveStarted(SoundBank.LastStandFromWave, "Five"));
            _bus.Publish(new WaveStarted(9, "Nine"));

            CollectionAssert.AreEqual(new[] { _gameplay, _gameplay, _lastStand, _lastStand }, _audio.Music);
        }

        [Test]
        public void WhenTheGameEnds_TheMusicFadesOut()
        {
            using GameMusic music = Start();

            _bus.Publish(new GameEnded(10, 10, false));

            Assert.AreEqual(1, _audio.MusicStops);
        }

        [Test]
        public void AfterDispose_NothingChanges()
        {
            GameMusic music = Start();
            music.Dispose();

            _bus.Publish(new WaveStarted(7, "Seven"));
            _bus.Publish(new GameEnded(10, 10, false));

            Assert.AreEqual(1, _audio.Music.Count);
            Assert.AreEqual(0, _audio.MusicStops);
        }

        [Test]
        public void TheTitle_PlaysTheTitleTune()
        {
            new TitleMusic(_audio, _bank).Start();

            CollectionAssert.AreEqual(new[] { _title }, _audio.Music);
        }

        [Test]
        public void MissingClips_PlayNothingAndDoNotThrow()
        {
            SoundBank empty = ScriptableObject.CreateInstance<SoundBank>();
            try
            {
                new GameMusic(_bus, _audio, empty).Start();
                new TitleMusic(_audio, empty).Start();

                Assert.IsEmpty(_audio.Music);
            }
            finally
            {
                Object.DestroyImmediate(empty);
            }
        }

        [Test]
        public void IfOneGameplayTuneIsMissing_TheOtherIsUsed()
        {
            Set("_lastStandMusic", null);

            Assert.AreSame(_gameplay, _bank.MusicForWave(8));
        }

        private GameMusic Start()
        {
            GameMusic music = new(_bus, _audio, _bank);
            music.Start();
            return music;
        }

        private void Set(string field, Object value)
        {
            typeof(SoundBank).GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(_bank, value);
        }
    }
}
