using NUnit.Framework;
using SpaceInvaders.Gameplay;

namespace SpaceInvaders.Tests
{
    public sealed class RunStateTests
    {
        [Test]
        public void A_new_run_starts_with_three_lives()
        {
            Assert.AreEqual(3, new RunState().Lives);
        }

        [Test]
        public void Losing_lives_raises_the_event_and_stops_at_zero()
        {
            RunState run = new();
            int last = -1;
            run.LivesChanged += lives => last = lives;

            Assert.AreEqual(2, run.LoseLife());
            Assert.AreEqual(1, run.LoseLife());
            Assert.AreEqual(0, run.LoseLife());
            Assert.AreEqual(0, run.LoseLife());

            Assert.AreEqual(0, last);
            Assert.IsTrue(run.IsOutOfLives);
        }

        [Test]
        public void Waves_count_up_from_one()
        {
            RunState run = new();

            Assert.AreEqual(1, run.NextWave());
            Assert.AreEqual(2, run.NextWave());
        }

        [Test]
        public void Reset_restores_lives_and_wave()
        {
            RunState run = new();
            run.NextWave();
            run.LoseAllLives();

            run.Reset();

            Assert.AreEqual(RunState.StartingLives, run.Lives);
            Assert.AreEqual(0, run.Wave);
        }
    }
}
