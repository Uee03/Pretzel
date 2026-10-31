using System;
using NUnit.Framework;
using Pretzel.Data;

namespace Pretzel.Cloud.Tests
{
    public sealed class ConflictResolverTests
    {
        private static ConflictInfo Info(long local, long cloud, bool deletion = false) => new(new DataKey("profile"), local, cloud, deletion);

        [Test]
        public void NewestWinsPicksTheLaterSaveTime()
        {
            NewestWinsConflictResolver resolver = new();
            Assert.That(resolver.Resolve(Info(10, 20)), Is.EqualTo(ConflictChoice.UseCloud));
            Assert.That(resolver.Resolve(Info(20, 10)), Is.EqualTo(ConflictChoice.UseLocal));
        }

        [Test]
        public void NewestWinsKeepsTheDeviceCopyOnATieAndWhenTheLocalTimeIsUnknown()
        {
            NewestWinsConflictResolver resolver = new();
            Assert.That(resolver.Resolve(Info(10, 10)), Is.EqualTo(ConflictChoice.UseLocal));
            // Unknown local time (0) loses to any cloud save, which is why first-run data should use KeepLocal when it must win.
            Assert.That(resolver.Resolve(Info(0, 5)), Is.EqualTo(ConflictChoice.UseCloud));
        }

        [Test]
        public void KeepLocalAndKeepCloudIgnoreTheTimes()
        {
            Assert.That(new KeepLocalConflictResolver().Resolve(Info(1, 99)), Is.EqualTo(ConflictChoice.UseLocal));
            Assert.That(new KeepCloudConflictResolver().Resolve(Info(99, 1)), Is.EqualTo(ConflictChoice.UseCloud));
            Assert.That(new KeepCloudConflictResolver().Resolve(Info(99, 1, true)), Is.EqualTo(ConflictChoice.UseCloud));
        }

        [Test]
        public void ConflictInfoComparesTimes()
        {
            Assert.That(Info(5, 9).CloudIsNewer, Is.True);
            Assert.That(Info(5, 9).LocalIsNewer, Is.False);
            Assert.That(Info(9, 5).LocalIsNewer, Is.True);
            Assert.That(Info(5, 5).LocalIsNewer, Is.False);
            Assert.That(Info(5, 5).CloudIsNewer, Is.False);
        }

        [Test]
        public void NewestWinsRejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new NewestWinsConflictResolver().Resolve(null));
        }
    }
}
