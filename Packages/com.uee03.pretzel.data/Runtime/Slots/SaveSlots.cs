using System;
using System.Collections.Generic;
using System.Threading;
using Pretzel.Core;
using UnityEngine;

namespace Pretzel.Data
{
    /// <summary>
    /// Numbered save slots for one save type. Each slot holds the data, a <see cref="SaveSlotInfo"/> written after the data,
    /// and an optional PNG thumbnail. The info file marks a slot as complete, so a menu never lists a half-written save.
    /// </summary>
    public sealed class SaveSlots<T>
    {
        private const string InfoName = "slot-info";
        private const string ThumbnailName = "slot-thumbnail";

        private readonly IDataStore _store;
        private readonly string _dataName;
        private readonly string _folder;

        /// <param name="store">Defaults to <see cref="DataIO.Default"/>, resolved on each call.</param>
        /// <param name="dataName">File name of the save data in each slot.</param>
        /// <param name="folder">Sub-folder that holds every slot.</param>
        public SaveSlots(IDataStore store = null, string dataName = "save", string folder = "Saves")
        {
            _store = store;
            _dataName = dataName;
            _folder = folder;
            // Validate once here rather than on first use.
            _ = new DataKey(dataName, 0, folder);
        }

        private IDataStore Store => _store ?? DataIO.Default;

        /// <summary>Writes the data, then the info. Fills <see cref="SaveSlotInfo.slot"/> and the save time.</summary>
        public SaveSlotInfo Save(int slot, T data, SaveSlotInfo info = null)
        {
            info = Stamp(slot, info);
            Store.Save(Key(_dataName, slot), data);
            Store.Save(Key(InfoName, slot), info);
            return info;
        }

        public async Awaitable<SaveSlotInfo> SaveAsync(int slot, T data, SaveSlotInfo info = null, CancellationToken cancellationToken = default)
        {
            info = Stamp(slot, info);
            await Store.SaveAsync(Key(_dataName, slot), data, cancellationToken);
            await Store.SaveAsync(Key(InfoName, slot), info, cancellationToken);
            return info;
        }

        public LoadResult<T> Load(int slot) => Store.Load<T>(Key(_dataName, slot));

        public Awaitable<LoadResult<T>> LoadAsync(int slot, CancellationToken cancellationToken = default)
        {
            return Store.LoadAsync<T>(Key(_dataName, slot), cancellationToken);
        }

        public LoadResult<SaveSlotInfo> LoadInfo(int slot) => Store.Load<SaveSlotInfo>(Key(InfoName, slot));

        /// <summary>True when the slot has both data and info.</summary>
        public bool Exists(int slot) => Store.Exists(Key(InfoName, slot)) && Store.Exists(Key(_dataName, slot));

        /// <summary>Every complete slot, newest first. Slots with unreadable info are skipped.</summary>
        public IReadOnlyList<SaveSlotInfo> ListInfos()
        {
            List<SaveSlotInfo> infos = new();
            foreach (int slot in Store.ListSlots(_folder))
            {
                if (Store.Exists(Key(_dataName, slot)) && LoadInfo(slot).TryGetValue(out SaveSlotInfo info) && info != null)
                {
                    infos.Add(info);
                }
            }

            infos.Sort((a, b) => b.savedAtUtcTicks.CompareTo(a.savedAtUtcTicks));
            return infos;
        }

        public bool TryGetMostRecent(out SaveSlotInfo info)
        {
            IReadOnlyList<SaveSlotInfo> infos = ListInfos();
            info = infos.Count > 0 ? infos[0] : null;
            return info != null;
        }

        /// <summary>The lowest slot from 0 to <paramref name="slotCount"/> - 1 without data, or -1 when all are used.</summary>
        public int FindFreeSlot(int slotCount)
        {
            for (int slot = 0; slot < slotCount; slot++)
            {
                if (!Exists(slot))
                {
                    return slot;
                }
            }

            return -1;
        }

        /// <returns>True when save data was deleted.</returns>
        public bool Delete(int slot)
        {
            Store.Delete(Key(InfoName, slot));
            Store.Delete(Key(ThumbnailName, slot));
            return Store.Delete(Key(_dataName, slot));
        }

        /// <param name="png">Encoded image bytes, for example from ImageConversion.EncodeToPNG.</param>
        public void SaveThumbnail(int slot, byte[] png)
        {
            Guard.NotNull(png, nameof(png));
            Store.Save(Key(ThumbnailName, slot), png);
        }

        /// <returns>The thumbnail bytes, or null when the slot has none.</returns>
        public byte[] LoadThumbnail(int slot) => Store.Load<byte[]>(Key(ThumbnailName, slot)).ValueOr(null);

        private DataKey Key(string name, int slot) => new(name, slot, _folder);

        private static SaveSlotInfo Stamp(int slot, SaveSlotInfo info)
        {
            Guard.InRange(slot, 0, int.MaxValue, nameof(slot));
            info ??= new SaveSlotInfo();
            info.slot = slot;
            info.savedAtUtcTicks = DateTime.UtcNow.Ticks;
            return info;
        }
    }
}
