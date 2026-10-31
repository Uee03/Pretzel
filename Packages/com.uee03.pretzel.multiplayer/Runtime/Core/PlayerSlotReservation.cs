using System;
using System.Collections.Generic;

namespace Pretzel.Multiplayer
{
    /// <summary>The player objects of one player key (one device), kept by the host so a dropped client can take them back within the grace window.</summary>
    public sealed class PlayerSlotReservation
    {
        private readonly List<int> _slots = new List<int>();
        private readonly List<ulong> _objectIds = new List<ulong>();

        public string Key { get; }

        /// <summary>The current NetworkManager client id, or null while disconnected.</summary>
        public ulong? ClientId { get; internal set; }

        public bool IsConnected => ClientId.HasValue;

        public DateTime ExpiresAtUtc { get; internal set; }

        /// <summary>One slot per local player, in LocalPlayerIndex order.</summary>
        public IReadOnlyList<int> Slots => _slots;

        /// <summary>NetworkObject ids that belong to this player and survive a disconnect.</summary>
        public IReadOnlyList<ulong> ObjectIds => _objectIds;

        internal PlayerSlotReservation(string key)
        {
            Key = key;
        }

        internal void AddSlot(int slot)
        {
            _slots.Add(slot);
        }

        internal void AddObject(ulong networkObjectId)
        {
            _objectIds.Add(networkObjectId);
        }
    }
}
