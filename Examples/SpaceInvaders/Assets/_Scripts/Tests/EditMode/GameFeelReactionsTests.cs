using NUnit.Framework;
using Pretzel.Events;
using Pretzel.Parallax;
using Pretzel.Vfx;
using SpaceInvaders.Feel;
using SpaceInvaders.Gameplay;
using UnityEngine;

namespace SpaceInvaders.Tests
{
    public sealed class GameFeelReactionsTests
    {
        private EventBus _bus;
        private FakeTimeScale _time;
        private FakeShaker _shaker;
        private GameObject _playerObject;
        private GameObject _starsObject;
        private ParallaxController _stars;
        private GameFeelReactions _reactions;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
            _time = new FakeTimeScale();
            _shaker = new FakeShaker();

            _playerObject = new GameObject("Player", typeof(SpriteRenderer));
            PlayerShip player = _playerObject.AddComponent<PlayerShip>();
            _playerObject.AddComponent<SpriteFlash>();
            _playerObject.AddComponent<PunchScale>();

            _starsObject = new GameObject("Stars");
            _stars = _starsObject.AddComponent<ParallaxController>();

            _reactions = new GameFeelReactions(_bus, _time, _shaker, player, _stars);
            _reactions.Start();
        }

        [TearDown]
        public void TearDown()
        {
            _reactions.Dispose();
            Object.DestroyImmediate(_playerObject);
            Object.DestroyImmediate(_starsObject);
        }

        [Test]
        public void AKill_FreezesBrieflyAndShakesLightly()
        {
            _bus.Publish(new InvaderDestroyed(InvaderKind.Crab, Vector3.zero, 20));

            Assert.AreEqual(new[] { GameFeelReactions.InvaderFreeze }, _time.Freezes);
            Assert.AreEqual(new[] { GameFeelReactions.InvaderTrauma }, _shaker.Hits);
            Assert.IsEmpty(_time.SlowMotions);
        }

        [Test]
        public void ABiggerKill_HitsHarder()
        {
            _bus.Publish(new UfoDestroyed(Vector3.zero, 100));

            Assert.AreEqual(GameFeelReactions.UfoFreeze, _time.Freezes[0]);
            Assert.AreEqual(GameFeelReactions.UfoTrauma, _shaker.Hits[0]);
            Assert.Greater(GameFeelReactions.UfoFreeze, GameFeelReactions.InvaderFreeze);
            Assert.Greater(GameFeelReactions.UfoTrauma, GameFeelReactions.InvaderTrauma);
        }

        [Test]
        public void ThePlayerBeingHit_IsTheBiggestHit_WithSlowMotionFlashAndPunch()
        {
            _bus.Publish(new PlayerHit(Vector3.zero));

            Assert.AreEqual(GameFeelReactions.PlayerFreeze, _time.Freezes[0]);
            Assert.AreEqual(GameFeelReactions.PlayerTrauma, _shaker.Hits[0]);
            Assert.AreEqual((GameFeelReactions.PlayerSlowScale, GameFeelReactions.PlayerSlowSeconds), _time.SlowMotions[0]);
            Assert.IsTrue(_playerObject.GetComponent<SpriteFlash>().IsFlashing, "The ship flashes when it is hit.");
            Assert.IsTrue(_playerObject.GetComponent<PunchScale>().IsPunching, "The ship pops when it is hit.");
            Assert.Greater(GameFeelReactions.PlayerTrauma, GameFeelReactions.UfoTrauma);
        }

        [Test]
        public void TheGameEnding_StartsALongSlowMotionAndAHardShake()
        {
            _bus.Publish(new GameEnded(1200, 1200, true));

            Assert.AreEqual((GameFeelReactions.GameOverSlowScale, GameFeelReactions.GameOverSlowSeconds), _time.SlowMotions[0]);
            Assert.AreEqual(GameFeelReactions.GameOverTrauma, _shaker.Hits[0]);
            Assert.IsEmpty(_time.Freezes, "The slow motion is the whole effect; a freeze on top would just stall.");
        }

        [Test]
        public void EachWave_SpeedsTheBackdropUp()
        {
            _bus.Publish(new WaveStarted(1, "First"));
            Assert.AreEqual(1f, _stars.AutoScrollScale, 0.0001f);

            _bus.Publish(new WaveStarted(4, "Fourth"));

            Assert.AreEqual(GameFeelReactions.ScrollScaleForWave(4), _stars.AutoScrollScale, 0.0001f);
            Assert.Greater(_stars.AutoScrollScale, 1f);
        }

        [TestCase(0, 1f)]
        [TestCase(1, 1f)]
        [TestCase(2, 1.12f)]
        [TestCase(10, 2.08f)]
        [TestCase(99, 2.08f)]
        public void TheBackdropSpeed_GrowsPerWaveAndIsCapped(int wave, float expected)
        {
            Assert.AreEqual(expected, GameFeelReactions.ScrollScaleForWave(wave), 0.0001f);
        }

        [Test]
        public void AfterDispose_EventsDoNothing()
        {
            _reactions.Dispose();

            _bus.Publish(new PlayerHit(Vector3.zero));
            _bus.Publish(new InvaderDestroyed(InvaderKind.Squid, Vector3.zero, 30));

            Assert.IsEmpty(_time.Freezes);
            Assert.IsEmpty(_shaker.Hits);
        }

        [Test]
        public void AShipWithoutFlashOrPunch_StillWorks()
        {
            Object.DestroyImmediate(_playerObject.GetComponent<SpriteFlash>());
            Object.DestroyImmediate(_playerObject.GetComponent<PunchScale>());
            _reactions.Dispose();
            _reactions = new GameFeelReactions(_bus, _time, _shaker, _playerObject.GetComponent<PlayerShip>(), _stars);
            _reactions.Start();

            Assert.DoesNotThrow(() => _bus.Publish(new PlayerHit(Vector3.zero)));
        }
    }
}
