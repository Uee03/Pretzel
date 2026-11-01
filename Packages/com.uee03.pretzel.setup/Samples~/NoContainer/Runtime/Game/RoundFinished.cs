namespace Pretzel.Samples.NoContainer
{
    public sealed class RoundFinished
    {
        public RoundFinished(int score, bool isBestScore)
        {
            Score = score;
            IsBestScore = isBestScore;
        }

        public int Score { get; }

        public bool IsBestScore { get; }
    }
}
