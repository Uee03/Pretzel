using NUnit.Framework;

namespace Pretzel.Core.Tests
{
    public sealed class DisposableActionTests
    {
        [Test]
        public void RunsOnce()
        {
            int calls = 0;
            DisposableAction action = new(() => calls++);
            action.Dispose();
            action.Dispose();
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(action.IsDisposed, Is.True);
        }

        [Test]
        public void EmptyIsSafe()
        {
            Assert.DoesNotThrow(() => DisposableAction.Empty.Dispose());
        }
    }
}
