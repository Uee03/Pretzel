namespace SpaceInvaders.Gameplay
{
    // Value type: event payload published on the EventBus; no allocation per publish.
    public readonly struct WaveStarted
    {
        public readonly int Number;
        public readonly string Title;

        public WaveStarted(int number, string title)
        {
            Number = number;
            Title = title;
        }
    }
}
