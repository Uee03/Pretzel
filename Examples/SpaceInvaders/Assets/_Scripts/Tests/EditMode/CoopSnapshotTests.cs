using NUnit.Framework;
using SpaceInvaders.Coop;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SpaceInvaders.Tests
{
    /// <summary>The replicated values must survive the wire exactly, and compare equal only when nothing changed, or NGO would send too much or too little.</summary>
    public sealed class CoopSnapshotTests
    {
        [Test]
        public void A_formation_snapshot_round_trips_through_the_wire()
        {
            FormationSnapshot sent = new()
            {
                Generation = 7,
                Wave = 3,
                DropRows = 2,
                Origin = new Vector2(-1.25f, 2.5f),
                StepIndex = 41,
                AliveLow = 0x00FF_00FF_1234_5678UL,
                AliveHigh = 0x0000_0000_0000_0003UL
            };

            FormationSnapshot received = RoundTrip(sent);

            Assert.AreEqual(sent, received);
            Assert.AreEqual(sent.AliveLow, received.AliveLow);
            Assert.AreEqual(sent.AliveHigh, received.AliveHigh);
            Assert.AreEqual(sent.Origin, received.Origin);
        }

        [Test]
        public void Any_changed_field_makes_formation_snapshots_unequal()
        {
            FormationSnapshot baseline = new() { Generation = 1, Wave = 1, StepIndex = 4, AliveLow = 0xFFUL };

            FormationSnapshot movedOn = baseline;
            movedOn.StepIndex = 5;
            FormationSnapshot killed = baseline;
            killed.AliveLow = 0x7FUL;
            FormationSnapshot nextWave = baseline;
            nextWave.Generation = 2;

            Assert.AreEqual(baseline, baseline);
            Assert.AreNotEqual(baseline, movedOn);
            Assert.AreNotEqual(baseline, killed);
            Assert.AreNotEqual(baseline, nextWave);
        }

        [Test]
        public void An_empty_snapshot_means_no_wave()
        {
            Assert.IsTrue(default(FormationSnapshot).IsEmpty);
            Assert.IsFalse(new FormationSnapshot { Wave = 1 }.IsEmpty);
        }

        [Test]
        public void A_player_stat_round_trips_and_knows_when_the_player_is_out()
        {
            CoopPlayerStat sent = new() { Slot = 1, Lives = 0, Score = 4200 };

            CoopPlayerStat received = RoundTrip(sent);

            Assert.AreEqual(sent, received);
            Assert.IsTrue(received.IsEliminated);
            Assert.IsFalse(new CoopPlayerStat { Lives = 1 }.IsEliminated);
        }

        [Test]
        public void A_ufo_snapshot_round_trips()
        {
            UfoSnapshot sent = new() { Visible = true, Position = new Vector2(3.5f, 3.45f) };

            UfoSnapshot received = RoundTrip(sent);

            Assert.AreEqual(sent, received);
            Assert.AreNotEqual(sent, new UfoSnapshot { Visible = false, Position = sent.Position });
        }

        private static T RoundTrip<T>(T value) where T : unmanaged, INetworkSerializable
        {
            FastBufferWriter writer = new(256, Allocator.Temp);
            try
            {
                writer.WriteNetworkSerializable(value);
                FastBufferReader reader = new(writer, Allocator.Temp);
                try
                {
                    reader.ReadNetworkSerializable(out T received);
                    return received;
                }
                finally
                {
                    reader.Dispose();
                }
            }
            finally
            {
                writer.Dispose();
            }
        }
    }
}
