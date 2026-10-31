namespace Pretzel.Samples.VContainerExample
{
    /// <summary>One instance behind two interfaces: readers cannot change the score.</summary>
    public sealed class Score : IScoreReader, IScoreWriter
    {
        public int Value { get; private set; }

        int IScoreReader.Score => Value;

        public void Add(int points) => Value += points;
    }
}
