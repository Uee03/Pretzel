namespace SpaceInvaders.Coop
{
    /// <summary>Lives and score of one player in a co-op match. Plain C#: the host keeps these and replicates a copy.</summary>
    public sealed class CoopPlayerRun
    {
        public CoopPlayerRun(int slot)
        {
            Slot = slot;
            Lives = CoopRunState.StartingLives;
        }

        public int Slot { get; }

        public int Lives { get; internal set; }

        public int Score { get; internal set; }

        public bool IsEliminated => Lives <= 0;
    }
}
