using System;
using System.IO;
using NUnit.Framework;

namespace Pretzel.Data.Tests
{
    public sealed class SaveSlotsTests
    {
        private string _folder;
        private DataStore _store;

        [Serializable]
        private sealed class Game
        {
            public int level;
        }

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "PretzelSlots_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            _store = new DataStore(DataRoots.Absolute(_folder));
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_folder, true);

        [Test]
        public void SaveLoadAndInfoRoundTrip()
        {
            SaveSlots<Game> slots = new(_store);
            SaveSlotInfo info = slots.Save(2, new Game { level = 7 }, new SaveSlotInfo { label = "Forest", playTimeSeconds = 90 });

            Assert.That(info.slot, Is.EqualTo(2));
            Assert.That(info.SavedAtUtc, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromMinutes(1)));
            Assert.That(slots.Exists(2), Is.True);
            Assert.That(slots.Load(2).Value.level, Is.EqualTo(7));
            Assert.That(slots.LoadInfo(2).Value.label, Is.EqualTo("Forest"));
            Assert.That(slots.Load(3).Status, Is.EqualTo(DataStatus.NotFound));
        }

        [Test]
        public void ListsCompleteSlotsNewestFirst()
        {
            SaveSlots<Game> slots = new(_store);
            slots.Save(0, new Game());
            slots.Save(1, new Game());
            _store.Save(new DataKey("slot-info", 0, "Saves"), new SaveSlotInfo { slot = 0, savedAtUtcTicks = 1 });
            // Data without info is a half-written slot and stays hidden.
            _store.Save(new DataKey("save", 5, "Saves"), new Game());

            Assert.That(slots.ListInfos(), Has.Count.EqualTo(2));
            Assert.That(slots.ListInfos()[0].slot, Is.EqualTo(1));
            Assert.That(slots.TryGetMostRecent(out SaveSlotInfo recent), Is.True);
            Assert.That(recent.slot, Is.EqualTo(1));
            Assert.That(slots.Exists(5), Is.False);
        }

        [Test]
        public void FindFreeSlotAndDelete()
        {
            SaveSlots<Game> slots = new(_store);
            slots.Save(0, new Game());
            slots.SaveThumbnail(0, new byte[] { 1, 2, 3 });
            Assert.That(slots.FindFreeSlot(3), Is.EqualTo(1));
            Assert.That(slots.LoadThumbnail(0), Is.EqualTo(new byte[] { 1, 2, 3 }));

            Assert.That(slots.Delete(0), Is.True);
            Assert.That(slots.Exists(0), Is.False);
            Assert.That(slots.LoadThumbnail(0), Is.Null);
            Assert.That(slots.FindFreeSlot(1), Is.EqualTo(0));
            Assert.That(slots.Delete(0), Is.False);
        }

        [Test]
        public void RejectsInvalidSlotsAndNames()
        {
            SaveSlots<Game> slots = new(_store);
            Assert.Throws<ArgumentOutOfRangeException>(() => slots.Save(-1, new Game()));
            Assert.Throws<ArgumentException>(() => new SaveSlots<Game>(_store, "../escape"));
        }
    }
}
