namespace Pretzel.Samples.NoContainer
{
    public sealed class TimeChanged
    {
        public TimeChanged(int secondsRemaining)
        {
            SecondsRemaining = secondsRemaining;
        }

        public int SecondsRemaining { get; }
    }
}
