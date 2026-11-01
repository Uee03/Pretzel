using UnityEngine;

namespace Pretzel.Cameras
{
    /// <summary>
    /// The trauma model of screen shake. Hits add trauma between 0 and 1, trauma decays on its own, and the shake
    /// strength is the trauma raised to a power (squared by default), so small hits are subtle, big hits are violent and
    /// stacking hits never exceed the maximum. It is plain C#, so it is testable and reusable by any camera.
    /// </summary>
    public sealed class TraumaShake
    {
        private readonly int _seed;
        private float _time;

        public TraumaShake(float decayPerSecond = 1.2f, float frequency = 25f, float exponent = 2f, int seed = 0)
        {
            DecayPerSecond = Mathf.Max(0f, decayPerSecond);
            Frequency = Mathf.Max(0f, frequency);
            Exponent = Mathf.Max(1f, exponent);
            _seed = seed;
        }

        public float DecayPerSecond { get; set; }

        /// <summary>How fast the noise changes, in cycles per second. 15 is a rumble, 30 is a jolt.</summary>
        public float Frequency { get; set; }

        public float Exponent { get; set; }

        /// <summary>0 when still, 1 at the most.</summary>
        public float Trauma { get; private set; }

        public bool IsShaking => Trauma > 0f;

        /// <summary>The strength applied to the maximum offsets: trauma to the power of <see cref="Exponent"/>.</summary>
        public float Strength => Exponent == 2f ? Trauma * Trauma : Mathf.Pow(Trauma, Exponent);

        public void Add(float amount)
        {
            Trauma = Mathf.Clamp01(Trauma + Mathf.Max(0f, amount));
        }

        /// <summary>Adds trauma but never beyond <paramref name="ceiling"/>, so a rumble does not drown out a big hit.</summary>
        public void AddUpTo(float amount, float ceiling)
        {
            if (Trauma < ceiling)
            {
                Trauma = Mathf.Min(Mathf.Clamp01(ceiling), Trauma + Mathf.Max(0f, amount));
            }
        }

        public void Clear()
        {
            Trauma = 0f;
        }

        /// <summary>Moves the noise and the decay forward. Call once per frame.</summary>
        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            _time += deltaTime;
            if (Trauma > 0f)
            {
                Trauma = Mathf.Max(0f, Trauma - DecayPerSecond * deltaTime);
            }
        }

        /// <summary>The offset for this moment, between minus and plus <paramref name="maxOffset"/> on each axis. Zero when still.</summary>
        public Vector3 Offset(Vector3 maxOffset)
        {
            if (Trauma <= 0f)
            {
                return Vector3.zero;
            }

            float strength = Strength;
            return new Vector3(
                Noise(0) * maxOffset.x * strength,
                Noise(1) * maxOffset.y * strength,
                Noise(2) * maxOffset.z * strength);
        }

        /// <summary>The roll in degrees for this moment, between minus and plus <paramref name="maxDegrees"/>. Zero when still.</summary>
        public float Roll(float maxDegrees)
        {
            return Trauma <= 0f ? 0f : Noise(3) * maxDegrees * Strength;
        }

        // Smooth noise in -1..1. Each channel reads its own row, so the axes do not move together.
        private float Noise(int channel)
        {
            return Mathf.PerlinNoise(_time * Frequency, _seed * 31.7f + channel * 17.3f) * 2f - 1f;
        }
    }
}
