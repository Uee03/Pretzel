namespace SpaceInvaders.Gameplay
{
    // Value type: event payload published on the EventBus; no allocation per publish.
    public readonly struct GameEnded
    {
        public readonly int Score;
        public readonly int HighScore;
        public readonly bool IsNewHighScore;

        public GameEnded(int score, int highScore, bool isNewHighScore)
        {
            Score = score;
            HighScore = highScore;
            IsNewHighScore = isNewHighScore;
        }
    }
}
