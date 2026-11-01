using System;
using NUnit.Framework;
using SpaceInvaders.Gameplay;
using UnityEngine;

namespace SpaceInvaders.Tests
{
    public sealed class ScoringAndWaveTests
    {
        [TestCase(InvaderKind.Squid, 30)]
        [TestCase(InvaderKind.Crab, 20)]
        [TestCase(InvaderKind.Octopus, 10)]
        public void Invaders_pay_by_kind(InvaderKind kind, int points)
        {
            Assert.AreEqual(points, ScoreTable.PointsFor(kind));
        }

        [Test]
        public void The_mystery_ship_cycles_through_its_payout_table()
        {
            Assert.AreEqual(ScoreTable.UfoPoints(3), ScoreTable.UfoPoints(3 + 16));
            Assert.AreEqual(300, ScoreTable.UfoPoints(8));
        }

        [Test]
        public void Negative_shot_counts_are_rejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ScoreTable.UfoPoints(-1));
        }

        [TestCase(1, 5, 0)]
        [TestCase(5, 5, 0)]
        [TestCase(6, 5, 1)]
        [TestCase(11, 5, 2)]
        [TestCase(500, 5, WaveSet.MaxDropRows)]
        public void The_formation_starts_lower_each_lap_but_never_beyond_the_limit(int wave, int waveCount, int expectedRows)
        {
            Assert.AreEqual(expectedRows, WaveSet.StartDropRows(wave, waveCount));
        }

        [Test]
        public void A_default_wave_is_a_full_classic_formation()
        {
            WaveDefinition wave = ScriptableObject.CreateInstance<WaveDefinition>();
            try
            {
                Assert.AreEqual(55, wave.InvaderCount);
                Assert.AreEqual((30 + 20 + 20 + 10 + 10) * 11, wave.TotalPoints());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(wave);
            }
        }

        [Test]
        public void An_empty_wave_set_explains_itself()
        {
            WaveSet set = ScriptableObject.CreateInstance<WaveSet>();
            try
            {
                Assert.Throws<InvalidOperationException>(() => set.GetWave(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(set);
            }
        }
    }
}
