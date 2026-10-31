namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Built by a registration delegate, because its seed is not a service.</summary>
    public sealed class DiceRoller : IDiceRoller
    {
        private readonly System.Random _random;

        public DiceRoller(int seed) => _random = new System.Random(seed);

        public int Roll() => _random.Next(1, 7);
    }
}
