namespace Pretzel.Samples.NoContainer
{
    public sealed class ScoreChanged
    {
        public ScoreChanged(int score)
        {
            Score = score;
        }

        public int Score { get; }
    }
}
