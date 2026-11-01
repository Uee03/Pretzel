namespace Pretzel.Samples.VContainerExample
{
    /// <summary>State that only exists while one level is running, registered in that level's child scope.</summary>
    public sealed class LevelState
    {
        public LevelState(int number) => Number = number;

        public int Number { get; }
    }
}
