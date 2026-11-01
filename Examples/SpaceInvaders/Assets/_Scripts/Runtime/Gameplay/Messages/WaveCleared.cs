namespace SpaceInvaders.Gameplay
{
    // Value type: event payload published on the EventBus; no allocation per publish.
    public readonly struct WaveCleared
    {
        public readonly int Number;

        public WaveCleared(int number)
        {
            Number = number;
        }
    }
}
