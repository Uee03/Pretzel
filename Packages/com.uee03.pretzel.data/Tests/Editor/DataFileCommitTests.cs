using System;
using System.IO;
using NUnit.Framework;

namespace Pretzel.Data.Tests
{
    public sealed class DataFileCommitTests
    {
        private string _folder;
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "PretzelCommit_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            _path = Path.Combine(_folder, "profile.json");
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_folder, true);

        [TestCase(false)]
        [TestCase(true)]
        public void FallbackRestoresOriginalWhenInstallingNewFileFails(bool keepBackup)
        {
            File.WriteAllText(_path, "{\"level\":7}");
            File.WriteAllText(_path + ".tmp", "{\"level\":8}");
            int moves = 0;
            Assert.Throws<IOException>(() => DataFileCommit.CommitFallback(_path + ".tmp", _path,
                keepBackup ? _path + ".bak" : null, (source, destination) =>
                {
                    if (++moves == 2)
                    {
                        throw new IOException("Simulated failure installing the new file.");
                    }
                    File.Move(source, destination);
                }));
            Assert.That(File.ReadAllText(_path), Is.EqualTo("{\"level\":7}"));
            Assert.That(File.Exists(_path + ".tmp"), Is.True, "New data remains available, but is not reported as committed.");
            Assert.That(File.Exists(_path + ".rollback"), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FallbackKeepsPreviousSaveUntilCommitCompletes(bool keepBackup)
        {
            File.WriteAllText(_path, "previous");
            File.WriteAllText(_path + ".tmp", "current");
            DataFileCommit.CommitFallback(_path + ".tmp", _path, keepBackup ? _path + ".bak" : null);
            Assert.That(File.ReadAllText(_path), Is.EqualTo("current"));
            Assert.That(File.Exists(_path + ".rollback"), Is.False);
            Assert.That(File.Exists(_path + ".bak"), Is.EqualTo(keepBackup));
            if (keepBackup)
            {
                Assert.That(File.ReadAllText(_path + ".bak"), Is.EqualTo("previous"));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InterruptedFallbackIsRecoveredOnLoadEvenWithoutBackups(bool keepBackup)
        {
            File.WriteAllText(_path + ".rollback", "{\"level\":7}");
            File.WriteAllText(_path + ".tmp", "{\"level\":8}");
            DataStore store = DataStoreFactory.Create(DataRoots.Absolute(_folder), DataFormat.PlainText, keepBackup: keepBackup);
            LoadResult<Save> loaded = store.Load<Save>("profile");
            Assert.That(loaded.Ok, Is.True, loaded.Error);
            Assert.That(loaded.Value.level, Is.EqualTo(7));
            Assert.That(File.ReadAllText(_path), Is.EqualTo("{\"level\":7}"));
            Assert.That(File.Exists(_path + ".rollback"), Is.False);
        }

        [Test]
        public void CompletedCommitWinsOverLeftoverRollback()
        {
            File.WriteAllText(_path, "{\"level\":8}");
            File.WriteAllText(_path + ".rollback", "{\"level\":7}");
            DataFileCommit.Recover(_path);
            Assert.That(File.ReadAllText(_path), Is.EqualTo("{\"level\":8}"));
            Assert.That(File.Exists(_path + ".rollback"), Is.False);
        }

        [Test]
        public void EnumerationAndExistsRecoverInterruptedFallback()
        {
            File.WriteAllText(_path + ".rollback", "{\"level\":7}");
            DataStore store = new(DataRoots.Absolute(_folder));
            Assert.That(store.List(), Has.Count.EqualTo(1));
            File.Move(_path, _path + ".rollback");
            Assert.That(store.Exists("profile"), Is.True);
            File.Move(_path, _path + ".rollback");
            Assert.That(store.Delete("profile"), Is.True);
            Assert.That(store.Exists("profile"), Is.False);
        }

        [Serializable]
        private sealed class Save
        {
            public int level;
        }
    }
}
