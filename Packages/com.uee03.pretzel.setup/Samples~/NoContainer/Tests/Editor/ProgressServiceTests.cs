using System.IO;
using NUnit.Framework;
using Pretzel.Data;
using UnityEngine;
using UnityEngine.TestTools;

namespace Pretzel.Samples.NoContainer.Tests
{
    public sealed class ProgressServiceTests
    {
        private string _folder;
        private DataStore _store;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "pretzel-nc-" + System.Guid.NewGuid().ToString("N"));
            _store = new DataStore(DataRoots.Absolute(_folder));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folder))
            {
                Directory.Delete(_folder, true);
            }
        }

        [Test]
        public void MissingFile_StartsEmptyAndIsNotProtected()
        {
            ProgressService progress = new(_store);

            progress.Load();

            Assert.AreEqual(0, progress.Current.bestScore);
            Assert.IsFalse(progress.IsSaveProtected);
        }

        [Test]
        public void RecordRound_KeepsTheBestScoreAndCountsRounds()
        {
            ProgressService progress = new(_store);

            Assert.IsTrue(progress.RecordRound(10));
            Assert.IsFalse(progress.RecordRound(7));
            Assert.IsTrue(progress.RecordRound(12));

            Assert.AreEqual(12, progress.Current.bestScore);
            Assert.AreEqual(3, progress.Current.roundsPlayed);
        }

        [Test]
        public void SavedProgress_LoadsInANewService()
        {
            ProgressService first = new(_store);
            first.RecordRound(21);
            first.Save();

            ProgressService second = new(_store);
            second.Load();

            Assert.AreEqual(21, second.Current.bestScore);
            Assert.AreEqual(1, second.Current.roundsPlayed);
        }

        [Test]
        public void Save_WritesNothingWhenNothingChanged()
        {
            ProgressService progress = new(_store);

            progress.Save();

            Assert.IsFalse(_store.Exists(ProgressService.SaveKey));
        }

        [Test]
        public void UnreadableFile_IsKeptAndNeverOverwritten()
        {
            Directory.CreateDirectory(_folder);
            string file = Path.Combine(_folder, "progress.json");
            File.WriteAllText(file, "{ this is not json");
            LogAssert.ignoreFailingMessages = true;
            try
            {
                ProgressService progress = new(_store);
                progress.Load();
                progress.RecordRound(5);
                progress.Save();

                Assert.IsTrue(progress.IsSaveProtected);
                Assert.AreEqual("{ this is not json", File.ReadAllText(file));
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
        }

        [Test]
        public void Changed_IsRaisedOnLoadAndOnRecord()
        {
            ProgressService progress = new(_store);
            int raised = 0;
            progress.Changed += _ => raised++;

            progress.Load();
            progress.RecordRound(1);

            Assert.AreEqual(2, raised);
        }
    }
}
