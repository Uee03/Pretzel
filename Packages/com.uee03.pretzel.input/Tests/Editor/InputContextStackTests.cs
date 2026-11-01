using NUnit.Framework;

namespace Pretzel.Inputs.Tests
{
    public sealed class InputContextStackTests
    {
        [Test]
        public void New_IsEmpty()
        {
            InputContextStack stack = new();

            Assert.IsNull(stack.Current);
            Assert.AreEqual(0, stack.Count);
        }

        [Test]
        public void Push_MakesTheContextCurrent()
        {
            InputContextStack stack = new();

            stack.Push("Gameplay");
            stack.Push("Menu");

            Assert.AreEqual("Menu", stack.Current);
        }

        [Test]
        public void Remove_OfTheTop_ReturnsToTheOneBelow()
        {
            InputContextStack stack = new();
            stack.Push("Gameplay");
            stack.Push("Menu");

            Assert.IsTrue(stack.Remove("Menu"));

            Assert.AreEqual("Gameplay", stack.Current);
        }

        [Test]
        public void Remove_OfABuriedContext_KeepsTheTop()
        {
            InputContextStack stack = new();
            stack.Push("Gameplay");
            stack.Push("Menu");

            stack.Remove("Gameplay");

            Assert.AreEqual("Menu", stack.Current);
            Assert.AreEqual(1, stack.Count);
        }

        [Test]
        public void Remove_OfAnUnknownContext_ReturnsFalse()
        {
            Assert.IsFalse(new InputContextStack().Remove("Nope"));
        }

        [Test]
        public void PushingAContextAlreadyOnTheStack_MovesItToTheTop()
        {
            InputContextStack stack = new();
            stack.Push("Gameplay");
            stack.Push("Menu");

            stack.Push("Gameplay");

            Assert.AreEqual("Gameplay", stack.Current);
            Assert.AreEqual(2, stack.Count);
        }

        [Test]
        public void PushingTheCurrentContext_RaisesNothing()
        {
            InputContextStack stack = new();
            stack.Push("Gameplay");
            int raised = 0;
            stack.Changed += _ => raised++;

            stack.Push("Gameplay");

            Assert.AreEqual(0, raised);
        }

        [Test]
        public void Changed_ReportsTheNewCurrentContext()
        {
            InputContextStack stack = new();
            string last = "unset";
            stack.Changed += context => last = context;

            stack.Push("Gameplay");
            Assert.AreEqual("Gameplay", last);

            stack.Remove("Gameplay");
            Assert.IsNull(last);
        }

        [Test]
        public void ContextsAreCaseSensitive()
        {
            InputContextStack stack = new();
            stack.Push("Gameplay");

            Assert.IsFalse(stack.Contains("gameplay"));
        }

        [TestCase(null)]
        [TestCase("")]
        public void Push_RejectsAnEmptyName(string name)
        {
            Assert.Throws<System.ArgumentException>(() => new InputContextStack().Push(name));
        }

        [Test]
        public void Clear_EmptiesTheStackAndRaisesOnce()
        {
            InputContextStack stack = new();
            stack.Push("a");
            stack.Push("b");
            int raised = 0;
            stack.Changed += _ => raised++;

            stack.Clear();
            stack.Clear();

            Assert.AreEqual(0, stack.Count);
            Assert.AreEqual(1, raised);
        }
    }
}
