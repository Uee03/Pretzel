using NUnit.Framework;
using SpaceInvaders.Gameplay;

namespace SpaceInvaders.Tests
{
    public sealed class MarchTempoTests
    {
        [Test]
        public void A_full_formation_marches_at_the_slowest_pace()
        {
            Assert.AreEqual(0.9f, MarchTempo.StepInterval(55, 55, 0.9f, 0.08f), 0.0001f);
        }

        [Test]
        public void The_last_invader_marches_at_the_fastest_pace()
        {
            Assert.AreEqual(0.08f, MarchTempo.StepInterval(1, 55, 0.9f, 0.08f), 0.0001f);
        }

        [Test]
        public void Fewer_invaders_never_march_slower()
        {
            float previous = float.MaxValue;
            for (int alive = 55; alive >= 1; alive--)
            {
                float interval = MarchTempo.StepInterval(alive, 55, 0.9f, 0.08f);
                Assert.LessOrEqual(interval, previous + 0.0001f);
                previous = interval;
            }
        }

        [Test]
        public void An_empty_formation_falls_back_to_the_slowest_pace()
        {
            Assert.AreEqual(0.9f, MarchTempo.StepInterval(0, 55, 0.9f, 0.08f), 0.0001f);
        }
    }
}
