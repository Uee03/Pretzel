using System;

namespace Pretzel.Samples.NoContainer
{
    /// <summary>The rules of one round: click as often as you can before the time runs out. No Unity types, so it is testable as it is.</summary>
    public sealed class GameRound
    {
        public GameRound(float duration)
        {
            if (duration <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(duration), "A round needs a positive duration.");
            }

            Duration = duration;
            TimeRemaining = duration;
        }

        public float Duration { get; }

        public float TimeRemaining { get; private set; }

        public int Score { get; private set; }

        public bool IsFinished => TimeRemaining <= 0f;

        /// <summary>Whole seconds left, rounded up, so the display reads 1 until the round ends.</summary>
        public int SecondsRemaining => (int)Math.Ceiling(TimeRemaining);

        /// <summary>Returns false once the round is finished, so late clicks do not count.</summary>
        public bool Click()
        {
            if (IsFinished)
            {
                return false;
            }

            Score++;
            return true;
        }

        /// <summary>Advances the clock. Returns true on the call that ends the round.</summary>
        public bool Tick(float deltaTime)
        {
            if (IsFinished || deltaTime <= 0f)
            {
                return false;
            }

            TimeRemaining = Math.Max(0f, TimeRemaining - deltaTime);
            return IsFinished;
        }
    }
}
