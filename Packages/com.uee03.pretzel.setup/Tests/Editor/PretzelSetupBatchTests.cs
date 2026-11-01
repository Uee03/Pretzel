using System;
using NUnit.Framework;
using Pretzel.Setup.Composition;

namespace Pretzel.Setup.Tests
{
    public sealed class PretzelSetupBatchTests
    {
        [Test]
        public void ArgumentsOverrideTheCompositionSettings()
        {
            CompositionSettings settings = new();

            bool changed = PretzelSetupBatch.ApplyOverrides(new[]
            {
                "Unity.exe", "-batchmode", "-pretzelNamespace", "My.Game", "-pretzelScope", "Match",
                "-pretzelServices", "events, pooling", "-pretzelStarter", "false"
            }, settings);

            Assert.That(changed, Is.True);
            Assert.That(settings.namespaceName, Is.EqualTo("My.Game"));
            Assert.That(settings.scopeName, Is.EqualTo("Match"));
            Assert.That(settings.pretzelServices, Is.EqualTo(new[] { "events", "pooling" }));
            Assert.That(settings.starterServices, Is.False);
        }

        [Test]
        public void NoArgumentsLeaveTheSettingsAlone()
        {
            CompositionSettings settings = new() { namespaceName = "Keep" };

            bool changed = PretzelSetupBatch.ApplyOverrides(new[] { "Unity.exe", "-batchmode" }, settings);

            Assert.That(changed, Is.False);
            Assert.That(settings.namespaceName, Is.EqualTo("Keep"));
        }

        [Test]
        public void NoneClearsThePretzelServices()
        {
            CompositionSettings settings = new();
            settings.pretzelServices.Add("events");

            PretzelSetupBatch.ApplyOverrides(new[] { "-pretzelServices", "none" }, settings);

            Assert.That(settings.pretzelServices, Is.Empty);
        }

        [Test]
        public void AnUnknownServiceNamesTheKnownOnes()
        {
            ArgumentException error = Assert.Throws<ArgumentException>(
                () => PretzelSetupBatch.ApplyOverrides(new[] { "-pretzelServices", "teleporter" }, new CompositionSettings()));

            Assert.That(error.Message, Does.Contain("teleporter").And.Contain("events"));
        }

        [Test]
        public void AMissingValueIsRejected()
        {
            Assert.Throws<ArgumentException>(() => PretzelSetupBatch.ApplyOverrides(new[] { "-pretzelScope", "-batchmode" }, new CompositionSettings()));
            Assert.Throws<ArgumentException>(() => PretzelSetupBatch.ApplyOverrides(new[] { "-pretzelNamespace" }, new CompositionSettings()));
        }

        [Test]
        public void AStarterFlagMustBeABoolean()
        {
            Assert.Throws<ArgumentException>(() => PretzelSetupBatch.ApplyOverrides(new[] { "-pretzelStarter", "maybe" }, new CompositionSettings()));
        }
    }
}
