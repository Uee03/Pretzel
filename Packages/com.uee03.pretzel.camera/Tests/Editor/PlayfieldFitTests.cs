using NUnit.Framework;
using UnityEngine;

namespace Pretzel.Cameras.Tests
{
    public sealed class PlayfieldFitTests
    {
        private static Vector2 Field => new(16f, 9f);

        [Test]
        public void Contain_OnTheSameAspect_FitsTheHeight()
        {
            Assert.AreEqual(4.5f, PlayfieldFit.OrthographicSize(Field, 16f / 9f, FitMode.Contain), 0.0001f);
        }

        [Test]
        public void Contain_OnANarrowScreen_FitsTheWidth()
        {
            // A 1:1 screen needs half the width as its half-height.
            Assert.AreEqual(8f, PlayfieldFit.OrthographicSize(Field, 1f, FitMode.Contain), 0.0001f);
        }

        [Test]
        public void Contain_OnAnUltrawideScreen_FitsTheHeight()
        {
            Assert.AreEqual(4.5f, PlayfieldFit.OrthographicSize(Field, 21f / 9f, FitMode.Contain), 0.0001f);
        }

        [Test]
        public void Cover_OnANarrowScreen_FitsTheHeight()
        {
            Assert.AreEqual(4.5f, PlayfieldFit.OrthographicSize(Field, 1f, FitMode.Cover), 0.0001f);
        }

        [Test]
        public void Cover_OnAnUltrawideScreen_FitsTheWidth()
        {
            Assert.AreEqual(16f / 2f / (21f / 9f), PlayfieldFit.OrthographicSize(Field, 21f / 9f, FitMode.Cover), 0.0001f);
        }

        [Test]
        public void Width_AlwaysFitsTheWidth()
        {
            Assert.AreEqual(4f, PlayfieldFit.OrthographicSize(Field, 2f, FitMode.Width), 0.0001f);
        }

        [Test]
        public void Height_AlwaysFitsTheHeight()
        {
            Assert.AreEqual(4.5f, PlayfieldFit.OrthographicSize(Field, 0.5f, FitMode.Height), 0.0001f);
        }

        [Test]
        public void Contain_ShowsAtLeastTheWholePlayfieldOnEveryAspect()
        {
            foreach (float aspect in new[] { 0.5f, 0.75f, 1f, 4f / 3f, 16f / 9f, 21f / 9f, 32f / 9f })
            {
                float size = PlayfieldFit.OrthographicSize(Field, aspect, FitMode.Contain);

                Assert.GreaterOrEqual(size * 2f, Field.y - 0.0001f, $"Aspect {aspect}: height");
                Assert.GreaterOrEqual(size * 2f * aspect, Field.x - 0.0001f, $"Aspect {aspect}: width");
            }
        }

        [Test]
        public void ADegenerateAspectDoesNotProduceInfinityOrNaN()
        {
            float size = PlayfieldFit.OrthographicSize(Field, 0f, FitMode.Contain);

            Assert.IsFalse(float.IsNaN(size) || float.IsInfinity(size));
        }

        [Test]
        public void VerticalFieldOfView_MatchesTheOrthographicHeightAtTheDistance()
        {
            float fov = PlayfieldFit.VerticalFieldOfView(Field, 10f, 16f / 9f, FitMode.Contain);

            // 2 * atan(4.5 / 10)
            Assert.AreEqual(48.46f, fov, 0.05f);
        }

        [Test]
        public void VerticalFieldOfView_GrowsOnANarrowScreen()
        {
            float wide = PlayfieldFit.VerticalFieldOfView(Field, 10f, 16f / 9f, FitMode.Contain);
            float narrow = PlayfieldFit.VerticalFieldOfView(Field, 10f, 1f, FitMode.Contain);

            Assert.Greater(narrow, wide);
        }
    }
}
