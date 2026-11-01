using NUnit.Framework;

namespace Pretzel.Inputs.Tests
{
    public sealed class InputBufferTests
    {
        [Test]
        public void WithoutAPress_NothingIsBuffered()
        {
            InputBuffer buffer = new(0.1f);

            Assert.IsFalse(buffer.Peek(5f));
            Assert.IsFalse(buffer.Consume(5f));
        }

        [Test]
        public void APress_CountsInsideTheWindow()
        {
            InputBuffer buffer = new(0.1f);
            buffer.Press(1f);

            Assert.IsTrue(buffer.Consume(1.08f));
        }

        [Test]
        public void APress_IsForgottenAfterTheWindow()
        {
            InputBuffer buffer = new(0.1f);
            buffer.Press(1f);

            Assert.IsFalse(buffer.Consume(1.11f));
        }

        [Test]
        public void Consume_UsesThePressUp()
        {
            InputBuffer buffer = new(0.1f);
            buffer.Press(1f);

            Assert.IsTrue(buffer.Consume(1.01f));
            Assert.IsFalse(buffer.Consume(1.02f), "One press must count once.");
        }

        [Test]
        public void Peek_DoesNotUseThePress()
        {
            InputBuffer buffer = new(0.1f);
            buffer.Press(1f);

            Assert.IsTrue(buffer.Peek(1.01f));
            Assert.IsTrue(buffer.Peek(1.02f));
        }

        [Test]
        public void ANewPress_RestartsTheWindow()
        {
            InputBuffer buffer = new(0.1f);
            buffer.Press(1f);
            buffer.Press(1.2f);

            Assert.IsTrue(buffer.Consume(1.25f));
        }

        [Test]
        public void Clear_ForgetsThePress()
        {
            InputBuffer buffer = new(0.1f);
            buffer.Press(1f);

            buffer.Clear();

            Assert.IsFalse(buffer.Peek(1f));
        }

        [Test]
        public void AZeroWindow_OnlyCountsTheSameInstant()
        {
            InputBuffer buffer = new(0f);
            buffer.Press(1f);

            Assert.IsTrue(buffer.Peek(1f));
            Assert.IsFalse(buffer.Peek(1.001f));
        }
    }
}
