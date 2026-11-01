using System;
using NUnit.Framework;

namespace Pretzel.Samples.NoContainer.Tests
{
    public sealed class GameRoundTests
    {
        [Test]
        public void NewRound_StartsWithFullTimeAndNoScore()
        {
            GameRound round = new(10f);

            Assert.AreEqual(10f, round.TimeRemaining);
            Assert.AreEqual(0, round.Score);
            Assert.IsFalse(round.IsFinished);
        }

        [Test]
        public void NonPositiveDuration_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameRound(0f));
        }

        [Test]
        public void Click_CountsWhileRunning()
        {
            GameRound round = new(5f);

            Assert.IsTrue(round.Click());
            Assert.IsTrue(round.Click());

            Assert.AreEqual(2, round.Score);
        }

        [Test]
        public void Click_IsIgnoredAfterTheRoundEnds()
        {
            GameRound round = new(1f);
            round.Click();
            round.Tick(1f);

            Assert.IsFalse(round.Click());
            Assert.AreEqual(1, round.Score);
        }

        [Test]
        public void Tick_ReportsTheEndExactlyOnce()
        {
            GameRound round = new(1f);

            Assert.IsFalse(round.Tick(0.5f));
            Assert.IsTrue(round.Tick(0.6f));
            Assert.IsFalse(round.Tick(0.5f));
            Assert.AreEqual(0f, round.TimeRemaining);
        }

        [Test]
        public void SecondsRemaining_RoundsUp()
        {
            GameRound round = new(3f);
            round.Tick(0.2f);

            Assert.AreEqual(3, round.SecondsRemaining);
            round.Tick(0.9f);
            Assert.AreEqual(2, round.SecondsRemaining);
        }

        [Test]
        public void Tick_IgnoresNegativeTime()
        {
            GameRound round = new(2f);

            round.Tick(-1f);

            Assert.AreEqual(2f, round.TimeRemaining);
        }
    }
}
