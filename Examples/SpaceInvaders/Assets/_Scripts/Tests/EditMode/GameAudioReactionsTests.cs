using NUnit.Framework;
using Pretzel.Events;
using SpaceInvaders.Audio;
using SpaceInvaders.Gameplay;
using UnityEngine;

namespace SpaceInvaders.Tests
{
    public sealed class GameAudioReactionsTests
    {
        private EventBus _bus;
        private FakeSfx _sfx;
        private FakeAudio _audio;
        private SoundBank _bank;
        private AudioClip _note;
        private GameAudioReactions _reactions;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
            _sfx = new FakeSfx();
            _audio = new FakeAudio();
            _note = AudioClip.Create("note", 100, 1, 44100, false);
            _bank = ScriptableObject.CreateInstance<SoundBank>();
            typeof(SoundBank).GetField("_march", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(_bank, new[] { _note });
            _reactions = new GameAudioReactions(_bus, _sfx, _audio, _bank);
            _reactions.Start();
        }

        [TearDown]
        public void TearDown()
        {
            _reactions.Dispose();
            Object.DestroyImmediate(_bank);
            Object.DestroyImmediate(_note);
        }

        [Test]
        public void EachGameEvent_PlaysItsSoundById()
        {
            _bus.Publish(new PlayerFired());
            _bus.Publish(new InvaderDestroyed(InvaderKind.Crab, Vector3.zero, 20));
            _bus.Publish(new UfoDestroyed(Vector3.zero, 100));
            _bus.Publish(new PlayerHit(Vector3.zero));
            _bus.Publish(new WaveCleared(1));
            _bus.Publish(new GameEnded(10, 10, false));

            CollectionAssert.AreEqual(
                new[] { GameSounds.PlayerShot, GameSounds.InvaderDestroyed, GameSounds.UfoDestroyed, GameSounds.PlayerHit, GameSounds.WaveCleared, GameSounds.GameOver },
                _sfx.Played);
        }

        [Test]
        public void TheMarch_PlaysItsNoteThroughTheAudioService()
        {
            _bus.Publish(new InvaderMarched(0));

            CollectionAssert.AreEqual(new[] { _note }, _audio.Clips);
            Assert.IsEmpty(_sfx.Played, "The march notes are clips, not library sounds.");
        }

        [Test]
        public void TheSoundIds_AreUniqueAndNonEmpty()
        {
            string[] ids =
            {
                GameSounds.PlayerShot, GameSounds.InvaderDestroyed, GameSounds.PlayerHit,
                GameSounds.UfoDestroyed, GameSounds.WaveCleared, GameSounds.GameOver
            };

            CollectionAssert.AllItemsAreUnique(ids);
            CollectionAssert.DoesNotContain(ids, string.Empty);
        }

        [Test]
        public void AfterDispose_NothingPlays()
        {
            _reactions.Dispose();

            _bus.Publish(new PlayerFired());

            Assert.IsEmpty(_sfx.Played);
        }
    }
}
