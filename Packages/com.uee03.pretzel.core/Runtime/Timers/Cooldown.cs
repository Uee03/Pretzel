namespace Pretzel.Core.Timers
{
    /// <summary>
    /// A reusable ability or action gate. <see cref="TryUse"/> succeeds when ready and starts the cooldown.
    /// The owner calls <see cref="Tick"/> with a delta time.
    /// </summary>
    public sealed class Cooldown
    {
        public float Duration { get; set; }

        public float Remaining { get; private set; }

        public bool IsReady => Remaining <= 0f;

        /// <summary>1 right after use, 0 when ready.</summary>
        public float RemainingNormalized => Duration <= 0f ? 0f : Remaining / Duration;

        public Cooldown(float duration)
        {
            Duration = Guard.InRange(duration, 0f, float.MaxValue, nameof(duration));
        }

        public bool TryUse()
        {
            if (!IsReady)
            {
                return false;
            }

            Remaining = Duration;
            return true;
        }

        /// <summary>Makes the cooldown ready now.</summary>
        public void Reset() => Remaining = 0f;

        public void Tick(float deltaTime)
        {
            if (Remaining > 0f)
            {
                Remaining = Remaining > deltaTime ? Remaining - deltaTime : 0f;
            }
        }
    }
}
