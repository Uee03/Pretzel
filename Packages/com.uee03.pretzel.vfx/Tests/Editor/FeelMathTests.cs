using NUnit.Framework;

namespace Pretzel.Vfx.Tests
{
    public sealed class FeelMathTests
    {
        [Test]
        public void FlashIntensity_StartsAtOneAndEndsAtZero()
        {
            Assert.AreEqual(1f, FeelMath.FlashIntensity(0f, 0.2f), 0.0001f);
            Assert.AreEqual(0f, FeelMath.FlashIntensity(0.2f, 0.2f), 0.0001f);
            Assert.AreEqual(0f, FeelMath.FlashIntensity(5f, 0.2f), 0.0001f);
        }

        [Test]
        public void FlashIntensity_FallsMonotonically()
        {
            float previous = 2f;
            for (float t = 0f; t <= 0.2f; t += 0.01f)
            {
                float intensity = FeelMath.FlashIntensity(t, 0.2f);

                Assert.LessOrEqual(intensity, previous);
                previous = intensity;
            }
        }

        [Test]
        public void FlashIntensity_WithNoDuration_IsAlreadyOver()
        {
            Assert.AreEqual(0f, FeelMath.FlashIntensity(0f, 0f));
        }

        [Test]
        public void Punch_StartsAtTheAmplitude()
        {
            Assert.AreEqual(0.3f, FeelMath.Punch(0f, 0.4f, 0.3f), 0.0001f);
        }

        [Test]
        public void Punch_SettlesToExactlyZero()
        {
            Assert.AreEqual(0f, FeelMath.Punch(0.4f, 0.4f, 0.3f), 0.0001f);
            Assert.AreEqual(0f, FeelMath.Punch(9f, 0.4f, 0.3f), 0.0001f);
        }

        [Test]
        public void Punch_SwingsBothWays()
        {
            bool positive = false;
            bool negative = false;
            for (float t = 0f; t < 0.4f; t += 0.005f)
            {
                float value = FeelMath.Punch(t, 0.4f, 0.3f, 2f);
                positive |= value > 0.01f;
                negative |= value < -0.01f;
            }

            Assert.IsTrue(positive && negative, "A punch must overshoot in both directions.");
        }

        [Test]
        public void Punch_NeverExceedsTheAmplitude()
        {
            for (float t = 0f; t <= 0.4f; t += 0.002f)
            {
                Assert.LessOrEqual(UnityEngine.Mathf.Abs(FeelMath.Punch(t, 0.4f, 0.3f)), 0.3001f);
            }
        }

        [Test]
        public void Progress_IsClamped()
        {
            Assert.AreEqual(0f, FeelMath.Progress(-1f, 2f));
            Assert.AreEqual(0.5f, FeelMath.Progress(1f, 2f), 0.0001f);
            Assert.AreEqual(1f, FeelMath.Progress(9f, 2f));
        }
    }
}
