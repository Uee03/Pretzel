using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Pretzel.Tween.Tests
{
    public sealed class EaseUtilityTests
    {
        private static IEnumerable<EaseType> AllEases() => (EaseType[])Enum.GetValues(typeof(EaseType));

        [TestCaseSource(nameof(AllEases))]
        public void StartsAtZeroAndEndsAtOne(EaseType ease)
        {
            Assert.That(EaseUtility.Evaluate(ease, 0f), Is.EqualTo(0f).Within(0.0001f), "start");
            Assert.That(EaseUtility.Evaluate(ease, 1f), Is.EqualTo(1f).Within(0.0001f), "end");
        }

        [TestCaseSource(nameof(AllEases))]
        public void ClampsTimeAndNeverReturnsNaN(EaseType ease)
        {
            Assert.That(EaseUtility.Evaluate(ease, -1f), Is.EqualTo(EaseUtility.Evaluate(ease, 0f)));
            Assert.That(EaseUtility.Evaluate(ease, 2f), Is.EqualTo(EaseUtility.Evaluate(ease, 1f)));
            for (int i = 0; i <= 20; i++)
            {
                Assert.That(float.IsNaN(EaseUtility.Evaluate(ease, i / 20f)), Is.False);
            }
        }

        [Test]
        public void InOutCurvesAreSymmetricAtTheMidpoint()
        {
            Assert.That(EaseUtility.Evaluate(EaseType.InOutQuad, 0.5f), Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(EaseUtility.Evaluate(EaseType.InOutSine, 0.5f), Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(EaseUtility.Evaluate(EaseType.InOutCubic, 0.5f), Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void OutElasticStartsAtZeroForAnyAmplitude()
        {
            Assert.That(EaseUtility.Evaluate(EaseType.OutElastic, 0f, 0.3f, 2f), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void LerpOvershootsForBackCurves()
        {
            Assert.That(EaseUtility.Lerp(EaseType.Linear, 10f, 20f, 0.5f), Is.EqualTo(15f).Within(0.0001f));
            bool overshoots = false;
            for (int i = 1; i < 20; i++)
            {
                overshoots |= EaseUtility.Lerp(EaseType.OutBack, 0f, 1f, i / 20f) > 1f;
            }

            Assert.That(overshoots, Is.True);
            Vector3 mid = EaseUtility.Lerp(EaseType.Linear, Vector3.zero, Vector3.one, 0.5f);
            Assert.That(Vector3.Distance(mid, Vector3.one * 0.5f), Is.LessThan(0.0001f));
        }
    }
}
