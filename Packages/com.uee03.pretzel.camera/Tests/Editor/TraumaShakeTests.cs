using NUnit.Framework;
using UnityEngine;

namespace Pretzel.Cameras.Tests
{
    public sealed class TraumaShakeTests
    {
        [Test]
        public void New_IsStill()
        {
            TraumaShake shake = new();

            Assert.IsFalse(shake.IsShaking);
            Assert.AreEqual(Vector3.zero, shake.Offset(Vector3.one));
            Assert.AreEqual(0f, shake.Roll(5f));
        }

        [Test]
        public void Add_StacksAndClampsToOne()
        {
            TraumaShake shake = new();

            shake.Add(0.4f);
            shake.Add(0.4f);
            shake.Add(0.9f);

            Assert.AreEqual(1f, shake.Trauma);
        }

        [Test]
        public void Add_IgnoresNegativeAmounts()
        {
            TraumaShake shake = new();
            shake.Add(0.5f);

            shake.Add(-0.4f);

            Assert.AreEqual(0.5f, shake.Trauma);
        }

        [Test]
        public void AddUpTo_NeverRaisesTraumaPastTheCeiling()
        {
            TraumaShake shake = new();

            shake.AddUpTo(0.9f, 0.3f);
            shake.AddUpTo(0.9f, 0.3f);

            Assert.AreEqual(0.3f, shake.Trauma, 0.0001f);
        }

        [Test]
        public void AddUpTo_DoesNotLowerATraumaAboveTheCeiling()
        {
            TraumaShake shake = new();
            shake.Add(0.8f);

            shake.AddUpTo(0.5f, 0.3f);

            Assert.AreEqual(0.8f, shake.Trauma, 0.0001f);
        }

        [Test]
        public void Strength_IsTheSquareOfTrauma()
        {
            TraumaShake shake = new();
            shake.Add(0.5f);

            Assert.AreEqual(0.25f, shake.Strength, 0.0001f);
        }

        [Test]
        public void Advance_DecaysTraumaToExactlyZero()
        {
            TraumaShake shake = new(decayPerSecond: 2f);
            shake.Add(1f);

            shake.Advance(0.25f);
            Assert.AreEqual(0.5f, shake.Trauma, 0.0001f);

            shake.Advance(5f);
            Assert.AreEqual(0f, shake.Trauma);
            Assert.IsFalse(shake.IsShaking);
        }

        [Test]
        public void Offset_StaysWithinTheMaximumScaledByStrength()
        {
            TraumaShake shake = new(decayPerSecond: 0f);
            shake.Add(0.6f);
            Vector3 max = new(2f, 3f, 0f);

            for (int i = 0; i < 400; i++)
            {
                shake.Advance(0.016f);
                Vector3 offset = shake.Offset(max);

                Assert.LessOrEqual(Mathf.Abs(offset.x), max.x * shake.Strength + 0.0001f);
                Assert.LessOrEqual(Mathf.Abs(offset.y), max.y * shake.Strength + 0.0001f);
                Assert.AreEqual(0f, offset.z);
            }
        }

        [Test]
        public void Offset_ActuallyMoves()
        {
            TraumaShake shake = new(decayPerSecond: 0f);
            shake.Add(1f);
            float lowest = float.MaxValue;
            float highest = float.MinValue;

            for (int i = 0; i < 200; i++)
            {
                shake.Advance(0.016f);
                float x = shake.Offset(Vector3.one).x;
                lowest = Mathf.Min(lowest, x);
                highest = Mathf.Max(highest, x);
            }

            Assert.Greater(highest - lowest, 0.5f);
        }

        [Test]
        public void SameSeedAndTime_GiveTheSameShake()
        {
            TraumaShake first = new(seed: 5);
            TraumaShake second = new(seed: 5);
            first.Add(1f);
            second.Add(1f);

            first.Advance(0.3f);
            second.Advance(0.3f);

            Assert.AreEqual(first.Offset(Vector3.one), second.Offset(Vector3.one));
        }

        [Test]
        public void DifferentSeeds_GiveDifferentShakes()
        {
            TraumaShake first = new(seed: 1);
            TraumaShake second = new(seed: 2);
            first.Add(1f);
            second.Add(1f);

            first.Advance(0.3f);
            second.Advance(0.3f);

            Assert.AreNotEqual(first.Offset(Vector3.one), second.Offset(Vector3.one));
        }

        [Test]
        public void Clear_StopsTheShakeAtOnce()
        {
            TraumaShake shake = new();
            shake.Add(1f);

            shake.Clear();

            Assert.IsFalse(shake.IsShaking);
        }

        [Test]
        public void Advance_IgnoresZeroAndNegativeTime()
        {
            TraumaShake shake = new(decayPerSecond: 1f);
            shake.Add(0.5f);

            shake.Advance(0f);
            shake.Advance(-1f);

            Assert.AreEqual(0.5f, shake.Trauma);
        }
    }
}
