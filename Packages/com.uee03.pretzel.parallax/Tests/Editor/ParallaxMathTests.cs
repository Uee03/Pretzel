using NUnit.Framework;
using UnityEngine;

namespace Pretzel.Parallax.Tests
{
    public sealed class ParallaxMathTests
    {
        [Test]
        public void FactorFromDepth_IsZeroAtTheFocalDistance()
        {
            Assert.AreEqual(0f, ParallaxMath.FactorFromDepth(10f, 10f), 0.0001f);
        }

        [Test]
        public void FactorFromDepth_ApproachesOneFarAway()
        {
            Assert.AreEqual(0.99f, ParallaxMath.FactorFromDepth(1000f, 10f), 0.0001f);
        }

        [Test]
        public void FactorFromDepth_IsNegativeInFrontOfTheFocalDistance()
        {
            Assert.AreEqual(-1f, ParallaxMath.FactorFromDepth(5f, 10f), 0.0001f);
        }

        [Test]
        public void FactorFromDepth_BehindTheCameraIsZero()
        {
            Assert.AreEqual(0f, ParallaxMath.FactorFromDepth(-3f, 10f));
        }

        [Test]
        public void Position_AppliesEachAxisFactorAndKeepsZ()
        {
            Vector3 position = ParallaxMath.Position(new Vector3(1f, 2f, 7f), new Vector3(10f, 20f, 5f), new Vector2(0.5f, 0.25f));

            Assert.AreEqual(new Vector3(6f, 7f, 7f), position);
        }

        [TestCase(0f, 0f, 4f, 0f)]
        [TestCase(9f, 0f, 4f, 8f)]
        [TestCase(-9f, 0f, 4f, -8f)]
        [TestCase(1.9f, 0f, 4f, 0f)]
        [TestCase(2.1f, 0f, 4f, 4f)]
        public void Wrap_KeepsTheLayerWithinHalfATileOfTheCamera(float camera, float layer, float tile, float expected)
        {
            Assert.AreEqual(expected, ParallaxMath.Wrap(camera, layer, tile), 0.0001f);
        }

        [Test]
        public void Wrap_WithoutATileSizeLeavesTheLayerAlone()
        {
            Assert.AreEqual(3f, ParallaxMath.Wrap(100f, 3f, 0f));
        }

        [Test]
        public void Wrap_NeverChangesWhereTheTilesAre()
        {
            float wrapped = ParallaxMath.Wrap(37.3f, 1.5f, 4f);
            float remainder = Mathf.Repeat(wrapped - 1.5f + 2f, 4f) - 2f;

            Assert.AreEqual(0f, remainder, 0.0001f);
        }

        [Test]
        public void Snap_RoundsToTheUnit()
        {
            Assert.AreEqual(0.5f, ParallaxMath.Snap(0.52f, 0.25f), 0.0001f);
            Assert.AreEqual(0.52f, ParallaxMath.Snap(0.52f, 0f));
        }
    }
}
