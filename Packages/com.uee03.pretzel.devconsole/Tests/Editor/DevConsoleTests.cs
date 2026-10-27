using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Pretzel.DevConsole.Tests
{
    public sealed class DevConsoleTests
    {
        [Test]
        public void TokenizeHonoursQuotes()
        {
            Assert.That(CommandRegistry.Tokenize("give  \"iron sword\" 2"), Is.EqualTo(new[] { "give", "iron sword", "2" }));
            Assert.That(CommandRegistry.Tokenize("say \"\""), Is.EqualTo(new[] { "say", "" }));
            Assert.That(CommandRegistry.Tokenize("   "), Is.Empty);
        }

        [Test]
        public void ExecuteRunsCaseInsensitiveCommands()
        {
            CommandRegistry registry = new();
            string[] received = null;
            registry.Register("Spawn", "Spawns things.", args => received = args);
            registry.Register("echo", "Echoes.", args => string.Join(" ", args));

            Assert.That(registry.Execute("spawn crate 3"), Is.Null);
            Assert.That(received, Is.EqualTo(new[] { "crate", "3" }));
            Assert.That(registry.Execute("echo a b"), Is.EqualTo("a b"));
            Assert.That(registry.Execute("nope"), Does.Contain("Unknown command"));
            Assert.That(registry.Execute("help"), Does.Contain("Spawn - Spawns things."));
        }

        [Test]
        public void HandlerExceptionsBecomeText()
        {
            CommandRegistry registry = new();
            registry.Register("fail", null, (Action<string[]>)(_ => throw new InvalidOperationException("bad state")));
            Assert.That(registry.Execute("fail"), Does.Contain("bad state"));
            Assert.Throws<ArgumentException>(() => registry.Register("two words", null, (Action<string[]>)(_ => { })));
        }

        [Test]
        public void LogBufferKeepsTheNewestEntriesAndCounts()
        {
            using LogBuffer buffer = new(3);
            int version = buffer.Version;
            buffer.Add(new LogEntry(LogType.Error, "e1", null, DateTime.Now));
            buffer.Add(new LogEntry(LogType.Warning, "w1", null, DateTime.Now));
            buffer.Add(new LogEntry(LogType.Log, "l1", null, DateTime.Now));
            buffer.Add(new LogEntry(LogType.Log, "l2", null, DateTime.Now));

            List<LogEntry> entries = new();
            buffer.CopyTo(entries);
            Assert.That(entries.ConvertAll(e => e.Message), Is.EqualTo(new[] { "w1", "l1", "l2" }));
            Assert.That(buffer.ErrorCount, Is.EqualTo(0), "The dropped error no longer counts.");
            Assert.That(buffer.WarningCount, Is.EqualTo(1));
            Assert.That(buffer.Version, Is.Not.EqualTo(version));

            buffer.CopyTo(entries, e => e.Type == LogType.Warning);
            Assert.That(entries, Has.Count.EqualTo(1));
            buffer.Clear();
            Assert.That(buffer.Count, Is.EqualTo(0));
        }

        [Test]
        public void AttachedBufferCapturesUnityLogs()
        {
            using LogBuffer buffer = new LogBuffer(10).Attach();
            Debug.Log("pretzel-console-probe");
            List<LogEntry> entries = new();
            buffer.CopyTo(entries, e => e.Message == "pretzel-console-probe");
            Assert.That(entries, Has.Count.EqualTo(1));
        }
    }
}
