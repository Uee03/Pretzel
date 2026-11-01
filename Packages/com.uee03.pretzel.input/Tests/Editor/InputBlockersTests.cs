using NUnit.Framework;

namespace Pretzel.Inputs.Tests
{
    public sealed class InputBlockersTests
    {
        [Test]
        public void New_IsNotBlocked()
        {
            Assert.IsFalse(new InputBlockers().IsBlocked);
        }

        [Test]
        public void ABlock_BlocksUntilItsReasonIsReleased()
        {
            InputBlockers blockers = new();

            blockers.Block("scene-load");
            Assert.IsTrue(blockers.IsBlocked);

            blockers.Unblock("scene-load");
            Assert.IsFalse(blockers.IsBlocked);
        }

        [Test]
        public void SeveralReasons_AllMustBeReleased()
        {
            InputBlockers blockers = new();
            blockers.Block("scene-load");
            blockers.Block("cutscene");

            blockers.Unblock("scene-load");
            Assert.IsTrue(blockers.IsBlocked);

            blockers.Unblock("cutscene");
            Assert.IsFalse(blockers.IsBlocked);
        }

        [Test]
        public void ABlockHeldTwice_NeedsOnlyOneRelease()
        {
            InputBlockers blockers = new();
            blockers.Block("cutscene");
            blockers.Block("cutscene");

            blockers.Unblock("cutscene");

            Assert.IsFalse(blockers.IsBlocked, "A reason counts once, so a repeated block cannot leave input stuck.");
        }

        [Test]
        public void ReleasingAReasonThatIsNotHeld_DoesNothing()
        {
            InputBlockers blockers = new();
            blockers.Block("cutscene");

            blockers.Unblock("scene-load");
            blockers.Unblock(null);

            Assert.IsTrue(blockers.IsBlocked);
        }

        [Test]
        public void Changed_FiresOnlyWhenTheStateChanges()
        {
            InputBlockers blockers = new();
            int blocked = 0;
            int unblocked = 0;
            blockers.Changed += state =>
            {
                if (state)
                {
                    blocked++;
                }
                else
                {
                    unblocked++;
                }
            };

            blockers.Block("a");
            blockers.Block("b");
            blockers.Unblock("a");
            blockers.Unblock("b");

            Assert.AreEqual(1, blocked);
            Assert.AreEqual(1, unblocked);
        }

        [TestCase(null)]
        [TestCase("")]
        public void Block_RejectsAnEmptyReason(string reason)
        {
            Assert.Throws<System.ArgumentException>(() => new InputBlockers().Block(reason));
        }

        [Test]
        public void Clear_ReleasesEverything()
        {
            InputBlockers blockers = new();
            blockers.Block("a");
            blockers.Block("b");

            blockers.Clear();

            Assert.IsFalse(blockers.IsBlocked);
            Assert.AreEqual(0, blockers.Count);
        }
    }
}
