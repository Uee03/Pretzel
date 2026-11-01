using NUnit.Framework;

namespace Pretzel.Vfx.Tests
{
    public sealed class TimeScaleStackTests
    {
        [Test]
        public void Empty_RunsAtFullSpeed()
        {
            TimeScaleStack stack = new();

            Assert.AreEqual(1f, stack.CurrentScale);
            Assert.IsFalse(stack.IsActive);
        }

        [Test]
        public void APush_SetsTheScaleUntilItExpires()
        {
            TimeScaleStack stack = new();
            stack.Push(0f, 0.1f);

            Assert.AreEqual(0f, stack.CurrentScale);
            stack.Advance(0.05f);
            Assert.AreEqual(0f, stack.CurrentScale);
            stack.Advance(0.06f);

            Assert.AreEqual(1f, stack.CurrentScale);
            Assert.IsFalse(stack.IsActive);
        }

        [Test]
        public void Overlapping_TakesTheSlowestScale()
        {
            TimeScaleStack stack = new();
            stack.Push(0.5f, 1f);
            stack.Push(0.2f, 0.1f);

            Assert.AreEqual(0.2f, stack.CurrentScale, 0.0001f);
            stack.Advance(0.2f);
            Assert.AreEqual(0.5f, stack.CurrentScale, 0.0001f, "The short request ended, the long one remains.");
        }

        [Test]
        public void Push_IgnoresNonPositiveDurations()
        {
            TimeScaleStack stack = new();

            Assert.AreEqual(0, stack.Push(0f, 0f));
            Assert.AreEqual(0, stack.Push(0f, -1f));
            Assert.IsFalse(stack.IsActive);
        }

        [Test]
        public void Push_ClampsTheScaleToZeroAndOne()
        {
            TimeScaleStack stack = new();
            stack.Push(-3f, 1f);
            Assert.AreEqual(0f, stack.CurrentScale);

            stack.Clear();
            stack.Push(4f, 1f);
            Assert.AreEqual(1f, stack.CurrentScale);
        }

        [Test]
        public void Cancel_RemovesOneRequest()
        {
            TimeScaleStack stack = new();
            int slow = stack.Push(0.1f, 5f);
            stack.Push(0.6f, 5f);

            Assert.IsTrue(stack.Cancel(slow));

            Assert.AreEqual(0.6f, stack.CurrentScale, 0.0001f);
            Assert.IsFalse(stack.Cancel(slow), "A request can only be cancelled once.");
        }

        [Test]
        public void Cancel_OfAnUnknownIdDoesNothing()
        {
            TimeScaleStack stack = new();

            Assert.IsFalse(stack.Cancel(42));
        }

        [Test]
        public void Clear_RestoresFullSpeedAtOnce()
        {
            TimeScaleStack stack = new();
            stack.Push(0f, 5f);
            stack.Push(0.3f, 5f);

            stack.Clear();

            Assert.AreEqual(1f, stack.CurrentScale);
            Assert.AreEqual(0, stack.ActiveCount);
        }

        [Test]
        public void Advance_IgnoresZeroAndNegativeTime()
        {
            TimeScaleStack stack = new();
            stack.Push(0f, 0.1f);

            stack.Advance(0f);
            stack.Advance(-1f);

            Assert.AreEqual(0f, stack.CurrentScale);
        }

        [Test]
        public void ExpiredRequests_AreReusedWithFreshIds()
        {
            TimeScaleStack stack = new();
            int first = stack.Push(0f, 0.1f);
            stack.Advance(1f);

            int second = stack.Push(0.5f, 1f);

            Assert.AreNotEqual(first, second);
            Assert.IsFalse(stack.Cancel(first), "An old id must not cancel a recycled request.");
            Assert.AreEqual(0.5f, stack.CurrentScale, 0.0001f);
        }

        [Test]
        public void ManyRequests_AllExpireInOrder()
        {
            TimeScaleStack stack = new();
            for (int i = 1; i <= 20; i++)
            {
                stack.Push(0f, i * 0.1f);
            }

            stack.Advance(1.05f);
            Assert.AreEqual(10, stack.ActiveCount);

            stack.Advance(10f);
            Assert.AreEqual(0, stack.ActiveCount);
            Assert.AreEqual(1f, stack.CurrentScale);
        }
    }
}
