namespace SpaceInvaders.Coop
{
    /// <summary>Where a co-op match is. The host decides it and every machine sees it.</summary>
    public enum CoopPhase : byte
    {
        Lobby = 0,
        Playing = 1,
        GameOver = 2
    }
}
