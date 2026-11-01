using NUnit.Framework;
using SpaceInvaders.Gameplay;
using UnityEngine;

namespace SpaceInvaders.Tests
{
    public sealed class MarchLogicTests
    {
        private const float Left = -7f;
        private const float Right = 7f;

        [Test]
        public void Steps_sideways_while_there_is_room()
        {
            MarchLogic march = new();

            Vector2 step = march.NextStep(-3f, 3f, Left, Right, 0.3f, 0.4f);

            Assert.AreEqual(new Vector2(0.3f, 0f), step);
            Assert.IsFalse(march.LastStepDropped);
        }

        [Test]
        public void Drops_and_turns_around_when_the_next_step_would_leave_the_field()
        {
            MarchLogic march = new();

            Vector2 step = march.NextStep(3f, 6.9f, Left, Right, 0.3f, 0.4f);

            Assert.AreEqual(new Vector2(0f, -0.4f), step);
            Assert.IsTrue(march.LastStepDropped);
            Assert.AreEqual(-1f, march.Direction);
        }

        [Test]
        public void Marches_left_after_the_drop_and_drops_again_at_the_left_edge()
        {
            MarchLogic march = new();
            march.NextStep(3f, 6.9f, Left, Right, 0.3f, 0.4f);

            Vector2 left = march.NextStep(-3f, 3f, Left, Right, 0.3f, 0.4f);
            Vector2 drop = march.NextStep(-6.9f, 0f, Left, Right, 0.3f, 0.4f);

            Assert.AreEqual(new Vector2(-0.3f, 0f), left);
            Assert.AreEqual(new Vector2(0f, -0.4f), drop);
            Assert.AreEqual(1f, march.Direction);
        }

        [Test]
        public void Reset_returns_to_marching_right()
        {
            MarchLogic march = new();
            march.NextStep(3f, 6.9f, Left, Right, 0.3f, 0.4f);

            march.Reset();

            Assert.AreEqual(1f, march.Direction);
            Assert.IsFalse(march.LastStepDropped);
        }
    }
}
