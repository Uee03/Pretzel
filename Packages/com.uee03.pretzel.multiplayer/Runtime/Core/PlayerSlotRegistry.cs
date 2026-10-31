using System;
using System.Collections.Generic;
using Pretzel.Core;

namespace Pretzel.Multiplayer
{
    /// <summary>
    /// Host-side bookkeeping of player slots. A disconnected player keeps its slots (and the objects registered on them) for the grace window,
    /// so a rejoin with the same key resumes instead of starting over. Pure C#: the clock is injected and no Unity types are used.
    /// The key is whatever identifies a device (the Authentication player id online). It is not a credential, so within a trusted co-op group
    /// any member could claim another member's key during the grace window; use a stronger scheme for competitive games.
    /// </summary>
    public sealed class PlayerSlotRegistry
    {
        private readonly int _capacity;
        private readonly TimeSpan _grace;
        private readonly Func<DateTime> _utcNow;
        private readonly Dictionary<string, PlayerSlotReservation> _byKey = new Dictionary<string, PlayerSlotReservation>(StringComparer.Ordinal);
        private readonly Dictionary<ulong, PlayerSlotReservation> _byClient = new Dictionary<ulong, PlayerSlotReservation>();
        private readonly bool[] _used;
        private int _disconnectedCount;

        public int Capacity => _capacity;

        public int UsedSlots
        {
            get
            {
                int used = 0;
                for (int i = 0; i < _used.Length; i++)
                {
                    if (_used[i])
                    {
                        used++;
                    }
                }

                return used;
            }
        }

        /// <summary>True while at least one disconnected player is waiting. The host only needs to poll <see cref="CollectExpired"/> then.</summary>
        public bool HasReservations => _disconnectedCount > 0;

        public PlayerSlotRegistry(int capacity, TimeSpan grace, Func<DateTime> utcNow = null)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _capacity = capacity;
            _grace = grace < TimeSpan.Zero ? TimeSpan.Zero : grace;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
            _used = new bool[capacity];
        }

        public PlayerSlotClaimResult Claim(string key, ulong clientId, int localPlayerCount)
        {
            Guard.NotNullOrEmpty(key, nameof(key));
            if (localPlayerCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(localPlayerCount));
            }

            if (_byKey.TryGetValue(key, out PlayerSlotReservation existing))
            {
                if (existing.IsConnected)
                {
                    return new PlayerSlotClaimResult(PlayerSlotClaimStatus.DuplicateKey, null);
                }

                existing.ClientId = clientId;
                _byClient[clientId] = existing;
                _disconnectedCount--;
                return new PlayerSlotClaimResult(PlayerSlotClaimStatus.Resumed, existing);
            }

            if (_capacity - UsedSlots < localPlayerCount)
            {
                return new PlayerSlotClaimResult(PlayerSlotClaimStatus.Full, null);
            }

            PlayerSlotReservation created = new PlayerSlotReservation(key) { ClientId = clientId };
            for (int slot = 0, assigned = 0; slot < _capacity && assigned < localPlayerCount; slot++)
            {
                if (!_used[slot])
                {
                    _used[slot] = true;
                    created.AddSlot(slot);
                    assigned++;
                }
            }

            _byKey[key] = created;
            _byClient[clientId] = created;
            return new PlayerSlotClaimResult(PlayerSlotClaimStatus.Claimed, created);
        }

        public bool TryGetByClient(ulong clientId, out PlayerSlotReservation reservation)
        {
            return _byClient.TryGetValue(clientId, out reservation);
        }

        public bool TryGetByKey(string key, out PlayerSlotReservation reservation)
        {
            return _byKey.TryGetValue(key, out reservation);
        }

        public void AddObject(string key, ulong networkObjectId)
        {
            if (_byKey.TryGetValue(key, out PlayerSlotReservation reservation))
            {
                reservation.AddObject(networkObjectId);
            }
        }

        /// <summary>
        /// Marks the client as gone. With a grace window the slots stay reserved and null is returned.
        /// Without one the reservation is released at once and returned, so the caller can despawn its objects.
        /// </summary>
        public PlayerSlotReservation Release(ulong clientId)
        {
            if (!_byClient.TryGetValue(clientId, out PlayerSlotReservation reservation))
            {
                return null;
            }

            _byClient.Remove(clientId);
            reservation.ClientId = null;
            if (_grace <= TimeSpan.Zero)
            {
                Free(reservation);
                return reservation;
            }

            reservation.ExpiresAtUtc = _utcNow().ToUniversalTime() + _grace;
            _disconnectedCount++;
            return null;
        }

        /// <summary>Frees every reservation whose grace window ended and appends it to <paramref name="expired"/>.</summary>
        public void CollectExpired(List<PlayerSlotReservation> expired)
        {
            Guard.NotNull(expired, nameof(expired));
            if (_disconnectedCount == 0)
            {
                return;
            }

            DateTime now = _utcNow().ToUniversalTime();
            foreach (KeyValuePair<string, PlayerSlotReservation> pair in _byKey)
            {
                PlayerSlotReservation reservation = pair.Value;
                if (!reservation.IsConnected && reservation.ExpiresAtUtc <= now)
                {
                    expired.Add(reservation);
                }
            }

            for (int i = 0; i < expired.Count; i++)
            {
                _disconnectedCount--;
                Free(expired[i]);
            }
        }

        public void Clear()
        {
            _byKey.Clear();
            _byClient.Clear();
            Array.Clear(_used, 0, _used.Length);
            _disconnectedCount = 0;
        }

        private void Free(PlayerSlotReservation reservation)
        {
            for (int i = 0; i < reservation.Slots.Count; i++)
            {
                _used[reservation.Slots[i]] = false;
            }

            _byKey.Remove(reservation.Key);
        }
    }
}
