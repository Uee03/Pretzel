using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Pretzel.Multiplayer.Tests
{
    public sealed class PlayerSlotRegistryTests
    {
        private DateTime _now;
        private PlayerSlotRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            _registry = new PlayerSlotRegistry(4, TimeSpan.FromSeconds(60), () => _now);
        }

        [Test]
        public void Claim_AssignsTheLowestFreeSlotsInOrder()
        {
            PlayerSlotClaimResult first = _registry.Claim("a", 0, 2);
            PlayerSlotClaimResult second = _registry.Claim("b", 1, 1);

            Assert.AreEqual(PlayerSlotClaimStatus.Claimed, first.Status);
            Assert.AreEqual(new[] { 0, 1 }, first.Reservation.Slots);
            Assert.AreEqual(new[] { 2 }, second.Reservation.Slots);
            Assert.AreEqual(3, _registry.UsedSlots);
        }

        [Test]
        public void Claim_WithoutEnoughSlots_IsFull()
        {
            _registry.Claim("a", 0, 3);

            PlayerSlotClaimResult result = _registry.Claim("b", 1, 2);

            Assert.AreEqual(PlayerSlotClaimStatus.Full, result.Status);
            Assert.IsNull(result.Reservation);
            Assert.IsFalse(result.Succeeded);
        }

        [Test]
        public void Claim_SameKeyWhileConnected_IsADuplicate()
        {
            _registry.Claim("a", 0, 1);

            Assert.AreEqual(PlayerSlotClaimStatus.DuplicateKey, _registry.Claim("a", 5, 1).Status);
        }

        [Test]
        public void Release_KeepsTheSlotsReservedForTheGraceWindow()
        {
            _registry.Claim("a", 7, 2);

            PlayerSlotReservation released = _registry.Release(7);

            Assert.IsNull(released, "Nothing is freed yet");
            Assert.IsTrue(_registry.HasReservations);
            Assert.AreEqual(2, _registry.UsedSlots);
            Assert.IsFalse(_registry.TryGetByClient(7, out PlayerSlotReservation _));
            Assert.IsTrue(_registry.TryGetByKey("a", out PlayerSlotReservation kept));
            Assert.IsFalse(kept.IsConnected);
        }

        [Test]
        public void Claim_WithinTheGraceWindow_ResumesTheSameSlotsAndObjects()
        {
            PlayerSlotReservation original = _registry.Claim("a", 7, 2).Reservation;
            _registry.AddObject("a", 100);
            _registry.AddObject("a", 101);
            _registry.Release(7);
            _now = _now.AddSeconds(30);

            PlayerSlotClaimResult resumed = _registry.Claim("a", 9, 2);

            Assert.AreEqual(PlayerSlotClaimStatus.Resumed, resumed.Status);
            Assert.AreSame(original, resumed.Reservation);
            Assert.AreEqual(new[] { 0, 1 }, resumed.Reservation.Slots);
            Assert.AreEqual(new ulong[] { 100, 101 }, resumed.Reservation.ObjectIds);
            Assert.AreEqual(9ul, resumed.Reservation.ClientId);
            Assert.IsFalse(_registry.HasReservations);
        }

        [Test]
        public void CollectExpired_FreesSlotsAfterTheGraceWindowAndReturnsTheObjects()
        {
            _registry.Claim("a", 7, 1);
            _registry.AddObject("a", 55);
            _registry.Release(7);
            List<PlayerSlotReservation> expired = new List<PlayerSlotReservation>();

            _now = _now.AddSeconds(59);
            _registry.CollectExpired(expired);
            Assert.IsEmpty(expired);

            _now = _now.AddSeconds(2);
            _registry.CollectExpired(expired);

            Assert.AreEqual(1, expired.Count);
            Assert.AreEqual(new ulong[] { 55 }, expired[0].ObjectIds);
            Assert.AreEqual(0, _registry.UsedSlots);
            Assert.IsFalse(_registry.HasReservations);
            Assert.AreEqual(PlayerSlotClaimStatus.Claimed, _registry.Claim("a", 8, 1).Status, "An expired player starts over");
        }

        [Test]
        public void Release_WithoutGrace_FreesImmediatelyAndReturnsTheReservation()
        {
            PlayerSlotRegistry noGrace = new PlayerSlotRegistry(2, TimeSpan.Zero, () => _now);
            noGrace.Claim("a", 3, 1);
            noGrace.AddObject("a", 12);

            PlayerSlotReservation released = noGrace.Release(3);

            Assert.IsNotNull(released);
            Assert.AreEqual(new ulong[] { 12 }, released.ObjectIds);
            Assert.AreEqual(0, noGrace.UsedSlots);
            Assert.IsFalse(noGrace.HasReservations);
        }

        [Test]
        public void ExpiredSlots_AreAvailableToOtherPlayers()
        {
            _registry.Claim("a", 1, 4);
            _registry.Release(1);
            Assert.AreEqual(PlayerSlotClaimStatus.Full, _registry.Claim("b", 2, 1).Status, "Reserved slots are not given away");

            _now = _now.AddMinutes(5);
            _registry.CollectExpired(new List<PlayerSlotReservation>());

            Assert.AreEqual(PlayerSlotClaimStatus.Claimed, _registry.Claim("b", 2, 1).Status);
        }

        [Test]
        public void Release_UnknownClient_DoesNothing()
        {
            Assert.IsNull(_registry.Release(99));
            Assert.IsFalse(_registry.HasReservations);
        }

        [Test]
        public void Clear_ForgetsEverything()
        {
            _registry.Claim("a", 1, 2);
            _registry.Release(1);

            _registry.Clear();

            Assert.AreEqual(0, _registry.UsedSlots);
            Assert.IsFalse(_registry.HasReservations);
            Assert.IsFalse(_registry.TryGetByKey("a", out PlayerSlotReservation _));
        }

        [Test]
        public void InvalidArguments_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerSlotRegistry(0, TimeSpan.Zero));
            Assert.Throws<ArgumentOutOfRangeException>(() => _registry.Claim("a", 1, 0));
            Assert.Throws<ArgumentException>(() => _registry.Claim(string.Empty, 1, 1));
        }
    }
}
