using UnityEngine;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Builds a short sine tone, so the audio demo needs no clip asset.</summary>
    public static class ToneClip
    {
        private const int SampleRate = 44100;

        public static AudioClip Create(float frequency, float seconds)
        {
            int count = Mathf.CeilToInt(SampleRate * seconds);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                // A short fade out avoids a click at the end.
                float envelope = 1f - (float)i / count;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / SampleRate) * 0.4f * envelope;
            }

            AudioClip clip = AudioClip.Create("Tone", count, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
