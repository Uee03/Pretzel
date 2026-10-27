using NUnit.Framework;
using UnityEngine;

namespace Pretzel.Audio.Tests
{
    public sealed class AudioTests
    {
        [TestCase(1f, 0f)]
        [TestCase(0.5f, -6.0206f)]
        [TestCase(0f, AudioMath.MinDecibels)]
        [TestCase(-1f, AudioMath.MinDecibels)]
        public void LinearToDecibels(float linear, float decibels)
        {
            Assert.That(AudioMath.LinearToDecibels(linear), Is.EqualTo(decibels).Within(0.001f));
        }

        [Test]
        public void DecibelsRoundTrip()
        {
            Assert.That(AudioMath.DecibelsToLinear(AudioMath.LinearToDecibels(0.3f)), Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(AudioMath.DecibelsToLinear(AudioMath.MinDecibels), Is.EqualTo(0f));
        }

        [Test]
        public void CueAvoidsRepeatingAClip()
        {
            AudioCue cue = ScriptableObject.CreateInstance<AudioCue>();
            AudioClip a = AudioClip.Create("a", 10, 1, 44100, false);
            AudioClip b = AudioClip.Create("b", 10, 1, 44100, false);
            try
            {
                Assert.That(cue.PickClip(), Is.Null);
                cue.SetClips(a, b);
                AudioClip last = cue.PickClip();
                for (int i = 0; i < 50; i++)
                {
                    AudioClip next = cue.PickClip();
                    Assert.That(next, Is.Not.SameAs(last));
                    last = next;
                }
            }
            finally
            {
                Object.DestroyImmediate(cue);
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
            }
        }

        [Test]
        public void DefaultHandleIsInvalid()
        {
            Assert.That(AudioHandle.None.IsValid, Is.False);
        }
    }
}
