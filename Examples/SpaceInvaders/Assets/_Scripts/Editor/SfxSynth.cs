using System;
using System.IO;
using UnityEngine;

namespace SpaceInvaders.Editor
{
    /// <summary>Tiny chip-style synth that renders the game's sound effects to samples and 16-bit WAV bytes. Deterministic, no assets needed.</summary>
    internal static class SfxSynth
    {
        public const int SampleRate = 44100;

        private const float PeakLevel = 0.8f;

        public static float[] Shot()
        {
            return Normalize(Sweep(Square, 1100f, 160f, 0.16f, 1f, 1.2f));
        }

        public static float[] InvaderDestroyed()
        {
            float[] noise = Noise(0.28f, 1f, 0.65f, 0.08f, 11);
            float[] thump = Sweep(Square, 220f, 60f, 0.28f, 0.45f, 1.5f);
            return Normalize(Mix(noise, thump));
        }

        public static float[] PlayerHit()
        {
            float[] noise = Noise(0.75f, 1f, 0.5f, 0.03f, 23);
            float[] rumble = Sweep(Sine, 110f, 38f, 0.75f, 0.9f, 1.1f);
            return Normalize(Mix(noise, rumble));
        }

        public static float[] UfoDestroyed()
        {
            return Normalize(Sequence(Saw, new[] { 523f, 659f, 784f, 1047f, 1319f }, 0.07f, 0.9f));
        }

        public static float[] WaveCleared()
        {
            return Normalize(Sequence(Square, new[] { 523f, 659f, 784f, 1047f }, 0.11f, 0.8f));
        }

        public static float[] GameOver()
        {
            return Normalize(Sequence(Saw, new[] { 392f, 349f, 311f, 262f, 196f }, 0.2f, 0.9f));
        }

        public static float[] MarchNote(float hz)
        {
            return Normalize(Sweep(Square, hz, hz * 0.97f, 0.09f, 1f, 0.8f));
        }

        public static byte[] ToWav(float[] samples)
        {
            using (MemoryStream stream = new())
            using (BinaryWriter writer = new(stream))
            {
                int dataBytes = samples.Length * 2;
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataBytes);
                writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(SampleRate);
                writer.Write(SampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(dataBytes);
                foreach (float sample in samples)
                {
                    writer.Write((short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
                }

                return stream.ToArray();
            }
        }

        private static float Square(float phase) => phase % 1f < 0.5f ? 1f : -1f;

        private static float Saw(float phase) => 2f * (phase % 1f) - 1f;

        private static float Sine(float phase) => Mathf.Sin(phase * Mathf.PI * 2f);

        private static float[] Sweep(Func<float, float> wave, float startHz, float endHz, float seconds, float amplitude, float decay)
        {
            float[] data = new float[(int)(seconds * SampleRate)];
            float phase = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)data.Length;
                phase += Mathf.Lerp(startHz, endHz, t) / SampleRate;
                data[i] = wave(phase) * Mathf.Pow(1f - t, decay) * amplitude;
            }

            return data;
        }

        private static float[] Noise(float seconds, float amplitude, float startCutoff, float endCutoff, int seed)
        {
            System.Random random = new(seed);
            float[] data = new float[(int)(seconds * SampleRate)];
            float filtered = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)data.Length;
                float white = (float)(random.NextDouble() * 2.0 - 1.0);
                filtered += Mathf.Lerp(startCutoff, endCutoff, t) * (white - filtered);
                data[i] = filtered * Mathf.Pow(1f - t, 1.4f) * amplitude * 2f;
            }

            return data;
        }

        private static float[] Sequence(Func<float, float> wave, float[] notes, float noteSeconds, float amplitude)
        {
            int noteLength = (int)(noteSeconds * SampleRate);
            float[] data = new float[noteLength * notes.Length];
            for (int note = 0; note < notes.Length; note++)
            {
                float[] tone = Sweep(wave, notes[note], notes[note], noteSeconds, amplitude, 0.6f);
                Array.Copy(tone, 0, data, note * noteLength, tone.Length);
            }

            return data;
        }

        private static float[] Mix(float[] a, float[] b)
        {
            float[] data = new float[Mathf.Max(a.Length, b.Length)];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (i < a.Length ? a[i] : 0f) + (i < b.Length ? b[i] : 0f);
            }

            return data;
        }

        private static float[] Normalize(float[] data)
        {
            float peak = 0f;
            foreach (float sample in data)
            {
                peak = Mathf.Max(peak, Mathf.Abs(sample));
            }

            if (peak <= 0f)
            {
                return data;
            }

            float gain = PeakLevel / peak;
            for (int i = 0; i < data.Length; i++)
            {
                data[i] *= gain;
            }

            return data;
        }
    }
}
